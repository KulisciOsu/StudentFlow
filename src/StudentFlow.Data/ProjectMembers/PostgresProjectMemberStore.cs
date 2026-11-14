using Dapper;
using Npgsql;
using StudentFlow.Core.Authentication;
using StudentFlow.Core.ProjectMembers;

namespace StudentFlow.Data.ProjectMembers;

/// <summary>Ukládá členství v projektech v PostgreSQL (tabulka project_member z V006 a sloupec added_by).</summary>
/// <remarks>
/// Zápisy jsou podmíněné: provedou se jen tehdy, je-li volající v okamžiku zápisu vedoucím projektu.
/// Odebrání maže pouze řádek členství; úkoly a další příspěvky člena zůstávají beze změny.
/// </remarks>
public sealed class PostgresProjectMemberStore(NpgsqlDataSource dataSource) : IProjectMemberStore
{
    private const string SelectMember = """
        SELECT pm.user_id AS UserId, u.display_name AS DisplayName, u.email AS Email,
               pm.is_leader AS IsLeader, pm.joined_at AS JoinedAt
        FROM project_member pm
        JOIN app_user u ON u.user_id = pm.user_id
        """;

    /// <summary>Podmínka, že daný uživatel je právě teď vedoucím projektu.</summary>
    private const string CallerIsLeader = """
        EXISTS (SELECT 1 FROM project_member leader
                WHERE leader.project_id = @ProjectId AND leader.user_id = @CallerId AND leader.is_leader)
        """;

    /// <inheritdoc />
    public async Task<ProjectMember?> FindMemberAsync(int projectId, long userId, CancellationToken cancellationToken = default)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        var row = await connection.QuerySingleOrDefaultAsync<MemberRow>(new CommandDefinition(
            SelectMember + " WHERE pm.project_id = @ProjectId AND pm.user_id = @UserId",
            new { ProjectId = projectId, UserId = userId }, cancellationToken: cancellationToken));
        return row?.ToMember();
    }

    /// <inheritdoc />
    public async Task<string?> FindTeamNameAsync(int projectId, CancellationToken cancellationToken = default)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<string?>(new CommandDefinition(
            "SELECT team_name FROM project WHERE project_id = @ProjectId",
            new { ProjectId = projectId }, cancellationToken: cancellationToken));
    }

    /// <inheritdoc />
    /// <remarks>Jména řadí podle české abecedy (ICU collation cs-x-icu); shodná jména podle ID kvůli stabilnímu pořadí.</remarks>
    public async Task<IReadOnlyList<ProjectMember>> ListAsync(int projectId, CancellationToken cancellationToken = default)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        var rows = await connection.QueryAsync<MemberRow>(new CommandDefinition($"""
            {SelectMember}
            WHERE pm.project_id = @ProjectId
            ORDER BY pm.is_leader DESC, u.display_name COLLATE "cs-x-icu", pm.user_id
            """, new { ProjectId = projectId }, cancellationToken: cancellationToken));
        return rows.Select(row => row.ToMember()).ToList();
    }

    /// <inheritdoc />
    public async Task<UserIdentity?> FindAccountByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        var row = await connection.QuerySingleOrDefaultAsync<AccountRow>(new CommandDefinition("""
            SELECT u.user_id AS Id, u.display_name AS DisplayName, u.email AS Email,
                   u.is_active AS IsActive, r.role_code AS AccountTypeCode
            FROM app_user u
            JOIN user_role ur ON ur.user_id = u.user_id
            JOIN app_role r ON r.role_id = ur.role_id
            WHERE r.role_code IN ('STUDENT', 'TEACHER')
              AND u.normalized_email = normalize_account_email(@Email)
            """, new { Email = email.Trim() }, cancellationToken: cancellationToken));
        return row?.ToIdentity();
    }

    /// <inheritdoc />
    public async Task<AddMemberResult> AddAsync(int projectId, long userId, long addedByUserId, CancellationToken cancellationToken = default)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        var parameters = new { ProjectId = projectId, UserId = userId, CallerId = addedByUserId };
        try
        {
            // Vloží jen tehdy, je-li přidávající stále vedoucím; existující členství nepřepíše.
            var row = await connection.QuerySingleOrDefaultAsync<MemberRow>(new CommandDefinition($"""
                WITH inserted AS (
                    INSERT INTO project_member (project_id, user_id, is_leader, added_by)
                    SELECT @ProjectId, @UserId, FALSE, @CallerId
                    WHERE {CallerIsLeader}
                    ON CONFLICT (project_id, user_id) DO NOTHING
                    RETURNING user_id, is_leader, joined_at
                )
                SELECT i.user_id AS UserId, u.display_name AS DisplayName, u.email AS Email,
                       i.is_leader AS IsLeader, i.joined_at AS JoinedAt
                FROM inserted i
                JOIN app_user u ON u.user_id = i.user_id
                """, parameters, cancellationToken: cancellationToken));
            if (row is not null) return new(AddMemberStatus.Added, row.ToMember());

            // Nic se nevložilo: buď už přidávající není vedoucím, nebo účet už je členem.
            var stillLeader = await connection.ExecuteScalarAsync<bool>(new CommandDefinition(
                $"SELECT {CallerIsLeader}", parameters, cancellationToken: cancellationToken));
            return new(stillLeader ? AddMemberStatus.AlreadyMember : AddMemberStatus.NotLeader, null);
        }
        catch (PostgresException exception) when (exception.SqlState == PostgresErrorCodes.CheckViolation
            && exception.ConstraintName == "project_member_student_only")
        {
            // Databázová pojistka migrace KAN-93: účet mezitím přestal být typu STUDENT.
            return new(AddMemberStatus.AccountNotStudent, null);
        }
    }

    /// <inheritdoc />
    public async Task<bool> RemoveAsync(int projectId, long userId, long removedByUserId, CancellationToken cancellationToken = default)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        var removed = await connection.ExecuteAsync(new CommandDefinition($"""
            DELETE FROM project_member
            WHERE project_id = @ProjectId AND user_id = @UserId AND NOT is_leader
              AND {CallerIsLeader}
            """, new { ProjectId = projectId, UserId = userId, CallerId = removedByUserId },
            cancellationToken: cancellationToken));
        return removed == 1;
    }

    private sealed class MemberRow
    {
        public long UserId { get; set; }
        public string DisplayName { get; set; } = "";
        public string Email { get; set; } = "";
        public bool IsLeader { get; set; }
        public DateTime JoinedAt { get; set; }
        public ProjectMember ToMember() => new(UserId, DisplayName, Email, IsLeader, new DateTimeOffset(JoinedAt));
    }

    private sealed class AccountRow
    {
        public long Id { get; set; }
        public string DisplayName { get; set; } = "";
        public string Email { get; set; } = "";
        public bool IsActive { get; set; }
        public string AccountTypeCode { get; set; } = "";
        public UserIdentity ToIdentity() => new(Id, DisplayName, Email, AccountTypeCodes.FromCode(AccountTypeCode), IsActive);
    }
}
