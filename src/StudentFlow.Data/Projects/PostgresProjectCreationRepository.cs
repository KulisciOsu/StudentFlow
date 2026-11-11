
using Dapper;
using Npgsql;
using StudentFlow.Core.Projects;

namespace StudentFlow.Data.Projects;

/// <summary>
/// Zajišťuje vytvoření projektu v databázi PostgreSQL.
/// </summary>
public sealed class PostgresProjectCreationRepository : IProjectCreationRepository
{
    private readonly NpgsqlDataSource _dataSource;

    /// <summary>
    /// Vytvoří repozitář pro zakládání projektů.
    /// </summary>
    public PostgresProjectCreationRepository(NpgsqlDataSource dataSource)
    {
        _dataSource = dataSource;
    }

    /// <inheritdoc />
    public async Task<int> CreateAsync(
        CreateProjectCommand command,
        long creatorUserId,
        CancellationToken cancellationToken)
    {
        const string insertProjectSql = """
            INSERT INTO project (name, description, team_name, due_date)
            VALUES (@Name, @Description, @TeamName, @DueDate)
            RETURNING project_id
            """;

        const string insertMemberSql = """
            INSERT INTO project_member (project_id, user_id, is_leader)
            VALUES (@ProjectId, @UserId, TRUE)
            """;

        const string insertColumnSql = """
            INSERT INTO board_column (project_id, title, position, is_done)
            VALUES (@ProjectId, @Title, @Position, @IsDone)
            """;

        var defaultColumns = new[]
        {
            new { Title = "K řešení", Position = 0, IsDone = false },
            new { Title = "Rozpracováno", Position = 1, IsDone = false },
            new { Title = "Ke kontrole", Position = 2, IsDone = false },
            new { Title = "Hotovo", Position = 3, IsDone = true }
        };

        await using var connection =
            await _dataSource.OpenConnectionAsync(cancellationToken);

        await using var transaction =
            await connection.BeginTransactionAsync(cancellationToken);

        var projectId = await connection.QuerySingleAsync<int>(
            new CommandDefinition(
                insertProjectSql,
                new
                {
                    Name = command.Name.Trim(),
                    Description = command.Description.Trim(),
                    TeamName = command.TeamName.Trim(),
                    command.DueDate
                },
                transaction,
                cancellationToken: cancellationToken));

        await connection.ExecuteAsync(
            new CommandDefinition(
                insertMemberSql,
                new
                {
                    ProjectId = projectId,
                    UserId = creatorUserId
                },
                transaction,
                cancellationToken: cancellationToken));

        foreach (var column in defaultColumns)
        {
            await connection.ExecuteAsync(
                new CommandDefinition(
                    insertColumnSql,
                    new
                    {
                        ProjectId = projectId,
                        column.Title,
                        column.Position,
                        column.IsDone
                    },
                    transaction,
                    cancellationToken: cancellationToken));
        }

        await transaction.CommitAsync(cancellationToken);

        return projectId;
    }
}
