using Dapper;
using Npgsql;

namespace StudentFlow.Api.Development;

/// <summary>
/// Zapojí vývojové účty člena a vedoucího do ukázkového projektu nástěnky, aby šla vyzkoušet oprávnění.
/// </summary>
/// <remarks>
/// Člen převezme úkoly Jana Nováka a vedoucí úkoly i vedení Petry Svobodové z ukázkových dat V005 a V007.
/// Bez tabulek nástěnky nebo bez ukázkového projektu nic nedělá. Opakované spuštění nic nezdvojí.
/// </remarks>
public static class DevelopmentBoardSeeder
{
    private const string DemoProjectName = "Rezervační systém knihovny";

    private const string DemoMemberEmail = "jan.novak@studentflow.local";

    private const string DemoLeaderEmail = "petra.svobodova@studentflow.local";

    /// <summary>
    /// Přidá člena a volitelně vedoucího do ukázkového projektu a přiřadí jim úkoly.
    /// </summary>
    /// <param name="dataSource">Zdroj připojení k databázi.</param>
    /// <param name="memberEmail">E-mail vývojového člena.</param>
    /// <param name="leaderEmail">E-mail vývojového vedoucího, nebo prázdný řetězec.</param>
    /// <param name="cancellationToken">Token pro zrušení databázových operací.</param>
    public static async Task SeedAsync(NpgsqlDataSource dataSource, string memberEmail, string leaderEmail, CancellationToken cancellationToken = default)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        const string boardExistsSql = """
            SELECT to_regclass('public.project_member') IS NOT NULL
               AND to_regclass('public.task') IS NOT NULL
            """;

        var boardExists = await connection.ExecuteScalarAsync<bool>(
            new CommandDefinition(boardExistsSql, transaction: transaction, cancellationToken: cancellationToken));

        if (!boardExists)
        {
            return;
        }

        const string projectSql = """
            SELECT project_id
            FROM project
            WHERE name = @Name
            ORDER BY project_id
            LIMIT 1
            """;

        var projectId = await connection.ExecuteScalarAsync<int?>(
            new CommandDefinition(projectSql, new { Name = DemoProjectName }, transaction, cancellationToken: cancellationToken));

        if (projectId is null)
        {
            return;
        }

        await JoinProjectAsync(connection, transaction, projectId.Value, memberEmail, false, DemoMemberEmail, cancellationToken);

        if (leaderEmail.Length != 0)
        {
            await JoinProjectAsync(connection, transaction, projectId.Value, leaderEmail, true, DemoLeaderEmail, cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
    }

    /// <summary>
    /// Přidá účet do projektu a převede na něj úkoly ukázkového řešitele.
    /// </summary>
    private static async Task JoinProjectAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, int projectId, string email, bool isLeader, string demoEmail, CancellationToken cancellationToken)
    {
        const string sql = """
            UPDATE project_member
            SET is_leader = FALSE
            WHERE @IsLeader
              AND project_id = @ProjectId
              AND is_leader
              AND user_id <> (SELECT user_id FROM app_user WHERE normalized_email = normalize_account_email(@Email));

            INSERT INTO project_member (project_id, user_id, is_leader)
            SELECT @ProjectId, user_id, @IsLeader
            FROM app_user
            WHERE normalized_email = normalize_account_email(@Email)
            ON CONFLICT (project_id, user_id) DO UPDATE SET is_leader = EXCLUDED.is_leader;

            UPDATE task
            SET assignee_id = (SELECT user_id FROM app_user WHERE normalized_email = normalize_account_email(@Email))
            WHERE project_id = @ProjectId
              AND assignee_id = (SELECT user_id FROM app_user WHERE email = @DemoEmail)
              AND EXISTS (SELECT 1 FROM app_user WHERE normalized_email = normalize_account_email(@Email));
            """;

        var parameters = new { ProjectId = projectId, Email = email, IsLeader = isLeader, DemoEmail = demoEmail };

        await connection.ExecuteAsync(new CommandDefinition(sql, parameters, transaction, cancellationToken: cancellationToken));
    }
}
