
using Npgsql;
using StudentFlow.Core.Kanban;
using StudentFlow.Data.Kanban;

namespace StudentFlow.Data.IntegrationTests;

/// <summary>
/// Testy repozitáře nástěnky proti PostgreSQL s migracemi V004 až V007.
/// </summary>
public class KanbanBoardRepositoryTests
{
    /// <summary>
    /// Ověří načtení ukázkového projektu, popisu, sloupců a úkolů.
    /// </summary>
    [Fact]
    public async Task Get_SeededProject_ReturnsBoard()
    {
        await using var database = await CreateDatabaseAsync();
        var repository = new KanbanBoardRepository(database.DataSource);

        var project = await repository.GetProjectAsync(1);
        var columns = await repository.GetColumnsAsync(1);
        var tasks = await repository.GetTasksAsync(1);
        var task = await repository.GetTaskAsync(2);

        Assert.NotNull(project);
        Assert.Equal("Rezervační systém knihovny", project.Name);
        Assert.Equal(string.Empty, project.Description);
        Assert.Equal(new DateOnly(2027, 1, 15), project.DueDate);
        Assert.Equal(
            new[] { "K řešení", "Rozpracováno", "Ke kontrole", "Hotovo" },
            columns.Select(column => column.Title));
        Assert.Equal(10, tasks.Count);
        Assert.NotNull(task);
        Assert.Equal("Tomáš Dvořák", task.Assignee);
        Assert.Null(await repository.GetProjectAsync(99));
        Assert.Null(await repository.GetTaskAsync(99));
    }

    /// <summary>
    /// Ověří, že přesun se uloží do databáze a pozice v obou sloupcích zůstanou souvislé.
    /// </summary>
    [Fact]
    public async Task SaveTasks_MovedTask_PersistsNewColumnAndPositions()
    {
        await using var database = await CreateDatabaseAsync();
        var repository = new KanbanBoardRepository(database.DataSource);
        var tasks = await repository.GetTasksAsync(1);
        var moved = TaskMoveRules.Move(tasks, 2, 3, 0);

        await repository.SaveTasksAsync(1, moved);

        var saved = await new KanbanBoardRepository(database.DataSource).GetTasksAsync(1);
        var task = saved.Single(item => item.Id == 2);

        Assert.Equal(3, task.ColumnId);
        Assert.Equal(0, task.Position);
        Assert.Equal(new[] { 0, 1 }, saved.Where(item => item.ColumnId == 1).Select(item => item.Position));
        Assert.Equal(new[] { 0, 1 }, saved.Where(item => item.ColumnId == 3).Select(item => item.Position));
    }

    /// <summary>
    /// Ověří, že databáze odmítne úkol ve sloupci jiného projektu a nic se neuloží.
    /// </summary>
    [Fact]
    public async Task SaveTasks_ColumnOfOtherProject_IsRejectedAndNothingIsSaved()
    {
        await using var database = await CreateDatabaseAsync();
        await database.ExecuteAsync("""
            INSERT INTO project (name, team_name) VALUES ('Evidence praxí', 'Tým Liška');
            INSERT INTO board_column (project_id, title, position) VALUES (2, 'K řešení', 0);
            """);
        var repository = new KanbanBoardRepository(database.DataSource);
        var tasks = await repository.GetTasksAsync(1);
        var changed = tasks
            .Select(task => task.Id == 2 ? WithPlace(task, 5, 0) : task)
            .ToList();

        var error = await Assert.ThrowsAsync<PostgresException>(() => repository.SaveTasksAsync(1, changed));

        Assert.Equal(PostgresErrorCodes.ForeignKeyViolation, error.SqlState);
        Assert.Equal(1, (await repository.GetTaskAsync(2))!.ColumnId);
    }

    /// <summary>
    /// Ověří, že databáze odmítne dva úkoly na stejné pozici ve sloupci a nic se neuloží.
    /// </summary>
    [Fact]
    public async Task SaveTasks_DuplicatePosition_IsRejectedAndNothingIsSaved()
    {
        await using var database = await CreateDatabaseAsync();
        var repository = new KanbanBoardRepository(database.DataSource);
        var tasks = await repository.GetTasksAsync(1);
        var changed = tasks
            .Select(task => task.Id == 2 ? WithPlace(task, 1, 0) : task)
            .ToList();

        var error = await Assert.ThrowsAsync<PostgresException>(() => repository.SaveTasksAsync(1, changed));

        Assert.Equal(PostgresErrorCodes.UniqueViolation, error.SqlState);
        Assert.Equal(1, (await repository.GetTaskAsync(2))!.Position);
    }

    /// <summary>
    /// Vytvoří testovací databázi s tabulkami a ukázkovými daty nástěnky.
    /// </summary>
    private static async Task<PostgresTestDatabase> CreateDatabaseAsync()
    {
        var database = await PostgresTestDatabase.CreateAsync();

        try
        {
            await database.ApplyMigrationAsync("V004__kanban_board.sql");
            await database.ApplyMigrationAsync("V005__seed_kanban_demo.sql");
            await database.ApplyMigrationAsync("V007__project_description.sql");

            return database;
        }
        catch
        {
            await database.DisposeAsync();
            throw;
        }
    }

    /// <summary>
    /// Vrátí kopii úkolu s jiným sloupcem a pozicí.
    /// </summary>
    private static KanbanBoardTaskDto WithPlace(KanbanBoardTaskDto task, int columnId, int position)
    {
        return new KanbanBoardTaskDto
        {
            Id = task.Id,
            ProjectId = task.ProjectId,
            ColumnId = columnId,
            Position = position,
            Title = task.Title,
            Priority = task.Priority,
            Assignee = task.Assignee,
            DueDate = task.DueDate,
        };
    }
}
