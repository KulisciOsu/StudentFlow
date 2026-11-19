using Npgsql;
using StudentFlow.Core.Authentication;
using StudentFlow.Core.Projects;
using StudentFlow.Data.Authentication;
using StudentFlow.Data.Projects;

namespace StudentFlow.Data.IntegrationTests;

/// <summary>
/// Integrační testy vytváření projektů proti PostgreSQL.
/// </summary>
public class ProjectCreationRepositoryTests
{
    /// <summary>
    /// Ověří vytvoření projektu, jeho vedoucího a výchozích Kanban sloupců.
    /// </summary>
    [Fact]
    public async Task CreateAsync_ValidCommand_CreatesProjectLeaderAndDefaultColumns()
    {
        await using var database = await CreateDatabaseAsync();

        var accountStore = new PostgresUserAccountStore(database.DataSource);

        var account = await accountStore.CreateAsync(
            new("Testovací uživatel", "test@example.test", "test-hash", AccountType.Member));

        var creatorUserId = account.User!.Id;

        var repository = new PostgresProjectCreationRepository(database.DataSource);

        var command = new CreateProjectCommand
        {
            Name = "  Nový projekt  ",
            TeamName = "  Vývojový tým  ",
            DueDate = new DateOnly(2027, 6, 30)
        };

        var projectId = await repository.CreateAsync(
            command,
            creatorUserId,
            CancellationToken.None);

        var projectName = await database.ScalarAsync<string>(
            $"SELECT name FROM project WHERE project_id = {projectId}");

        var teamName = await database.ScalarAsync<string>(
            $"SELECT team_name FROM project WHERE project_id = {projectId}");

        var dueDate = await database.ScalarAsync<DateOnly>(
            $"SELECT due_date FROM project WHERE project_id = {projectId}");

        var leaderCount = await database.ScalarAsync<long>(
            $"""
             SELECT count(*)
             FROM project_member
             WHERE project_id = {projectId}
               AND user_id = {creatorUserId}
               AND is_leader = TRUE
             """);

        var columnCount = await database.ScalarAsync<long>(
            $"SELECT count(*) FROM board_column WHERE project_id = {projectId}");

        var columnTitles = await database.ScalarAsync<string>(
            $"""
             SELECT string_agg(title, '|' ORDER BY position)
             FROM board_column
             WHERE project_id = {projectId}
             """);

        var completedColumnCount = await database.ScalarAsync<long>(
            $"""
             SELECT count(*)
             FROM board_column
             WHERE project_id = {projectId}
               AND is_done = TRUE
             """);

        Assert.Equal("Nový projekt", projectName);
        Assert.Equal("Vývojový tým", teamName);
        Assert.Equal(new DateOnly(2027, 6, 30), dueDate);

        Assert.Equal(1L, leaderCount);

        Assert.Equal(4L, columnCount);

        Assert.Equal(
            "K řešení|Rozpracováno|Ke kontrole|Hotovo",
            columnTitles);

        Assert.Equal(1L, completedColumnCount);
    }

    /// <summary>
    /// Ověří, že při chybě během vytváření sloupců dojde k vrácení celé transakce.
    /// </summary>
    [Fact]
    public async Task CreateAsync_ColumnCreationFails_RollsBackEntireProject()
    {
        await using var database = await CreateDatabaseAsync();

        var accountStore = new PostgresUserAccountStore(database.DataSource);

        var account = await accountStore.CreateAsync(
            new("Testovací uživatel", "rollback@example.test", "test-hash", AccountType.Member));

        var creatorUserId = account.User!.Id;

        await database.ExecuteAsync("""
            CREATE FUNCTION reject_board_column_insert()
            RETURNS trigger
            LANGUAGE plpgsql
            AS $$
            BEGIN
                RAISE EXCEPTION 'Simulovaná chyba při vytváření sloupce.';
            END;
            $$;

            CREATE TRIGGER reject_board_column_insert_trigger
            BEFORE INSERT ON board_column
            FOR EACH ROW
            EXECUTE FUNCTION reject_board_column_insert();
            """);

        var repository = new PostgresProjectCreationRepository(database.DataSource);

        var command = new CreateProjectCommand
        {
            Name = "Projekt pro test rollbacku",
            TeamName = "Testovací tým"
        };

        await Assert.ThrowsAsync<PostgresException>(
            () => repository.CreateAsync(
                command,
                creatorUserId,
                CancellationToken.None));

        var projectCount = await database.ScalarAsync<long>(
            "SELECT count(*) FROM project");

        var membershipCount = await database.ScalarAsync<long>(
            "SELECT count(*) FROM project_member");

        var columnCount = await database.ScalarAsync<long>(
            "SELECT count(*) FROM board_column");

        Assert.Equal(0L, projectCount);
        Assert.Equal(0L, membershipCount);
        Assert.Equal(0L, columnCount);
    }

    /// <summary>
    /// Připraví izolovanou databázi s potřebnými migracemi.
    /// </summary>
    private static async Task<PostgresTestDatabase> CreateDatabaseAsync()
    {
        return await PostgresTestDatabase.CreateAsync(dashboard: true);
    }
}