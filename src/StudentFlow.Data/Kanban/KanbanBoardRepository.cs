using Dapper;
using Npgsql;
using StudentFlow.Core.Kanban;

namespace StudentFlow.Data.Kanban;

/// <summary>
/// Přístup k datům nástěnky v databázi PostgreSQL přes Dapper.
/// </summary>
public class KanbanBoardRepository : IKanbanBoardRepository
{
    private const string TaskSelect = """
        SELECT task.task_id AS Id,
               task.project_id AS ProjectId,
               task.column_id AS ColumnId,
               task.position AS Position,
               task.title AS Title,
               task.priority AS Priority,
               app_user.display_name AS Assignee,
               task.assignee_id AS AssigneeId,
               task.due_date AS DueDate
        FROM task
        JOIN app_user ON app_user.user_id = task.assignee_id
        """;

    private readonly NpgsqlDataSource _dataSource;

    /// <summary>
    /// Vytvoří repozitář nástěnky.
    /// </summary>
    /// <param name="dataSource">Zdroj připojení k databázi.</param>
    public KanbanBoardRepository(NpgsqlDataSource dataSource)
    {
        _dataSource = dataSource;
    }

    /// <inheritdoc />
    public async Task<KanbanProjectDto?> GetProjectAsync(int projectId)
    {
        const string sql = """
            SELECT project_id AS Id,
                   name AS Name,
                   team_name AS TeamName,
                   due_date AS DueDate
            FROM project
            WHERE project_id = @ProjectId
            """;

        await using var connection = await _dataSource.OpenConnectionAsync();
        var row = await connection.QuerySingleOrDefaultAsync<ProjectRow>(sql, new { ProjectId = projectId });

        if (row is null)
        {
            return null;
        }

        return new KanbanProjectDto
        {
            Id = row.Id,
            Name = row.Name,
            TeamName = row.TeamName,
            DueDate = ToDateOnly(row.DueDate),
        };
    }

    /// <inheritdoc />
    public async Task<List<KanbanBoardColumnDto>> GetColumnsAsync(int projectId)
    {
        const string sql = """
            SELECT column_id AS Id,
                   project_id AS ProjectId,
                   title AS Title,
                   is_done AS IsDone
            FROM board_column
            WHERE project_id = @ProjectId
            ORDER BY position
            """;

        await using var connection = await _dataSource.OpenConnectionAsync();
        var rows = await connection.QueryAsync<ColumnRow>(sql, new { ProjectId = projectId });

        return rows.Select(ToColumn).ToList();
    }

    /// <inheritdoc />
    public async Task<KanbanBoardColumnDto?> GetColumnAsync(int columnId)
    {
        const string sql = """
            SELECT column_id AS Id,
                   project_id AS ProjectId,
                   title AS Title,
                   is_done AS IsDone
            FROM board_column
            WHERE column_id = @ColumnId
            """;

        await using var connection = await _dataSource.OpenConnectionAsync();
        var row = await connection.QuerySingleOrDefaultAsync<ColumnRow>(sql, new { ColumnId = columnId });

        if (row is null)
        {
            return null;
        }

        return ToColumn(row);
    }

    /// <inheritdoc />
    public async Task<List<KanbanBoardTaskDto>> GetTasksAsync(int projectId)
    {
        const string sql = TaskSelect + """

            WHERE task.project_id = @ProjectId
            ORDER BY task.column_id, task.position
            """;

        await using var connection = await _dataSource.OpenConnectionAsync();
        var rows = await connection.QueryAsync<TaskRow>(sql, new { ProjectId = projectId });

        return rows.Select(ToTask).ToList();
    }

    /// <inheritdoc />
    public async Task<KanbanBoardTaskDto?> GetTaskAsync(int taskId)
    {
        const string sql = TaskSelect + """

            WHERE task.task_id = @TaskId
            """;

        await using var connection = await _dataSource.OpenConnectionAsync();
        var row = await connection.QuerySingleOrDefaultAsync<TaskRow>(sql, new { TaskId = taskId });

        if (row is null)
        {
            return null;
        }

        return ToTask(row);
    }

    /// <inheritdoc />
    public async Task<bool?> GetMembershipAsync(int projectId, long userId)
    {
        const string sql = """
            SELECT is_leader
            FROM project_member
            WHERE project_id = @ProjectId
              AND user_id = @UserId
            """;

        await using var connection = await _dataSource.OpenConnectionAsync();

        return await connection.QuerySingleOrDefaultAsync<bool?>(sql, new { ProjectId = projectId, UserId = userId });
    }

    /// <inheritdoc />
    /// <remarks>
    /// Všechny změny se uloží v jedné transakci. Jedinečnost pozic se kontroluje až při potvrzení,
    /// takže úkoly lze přeskládat v libovolném pořadí. Při chybě se neuloží nic.
    /// </remarks>
    public async Task SaveTasksAsync(int projectId, List<KanbanBoardTaskDto> tasks)
    {
        const string sql = """
            UPDATE task
            SET column_id = @ColumnId,
                position = @Position
            WHERE task_id = @Id
              AND project_id = @ProjectId
            """;

        var parameters = tasks
            .Select(task => new { task.Id, task.ColumnId, task.Position, ProjectId = projectId })
            .ToList();

        await using var connection = await _dataSource.OpenConnectionAsync();
        await using var transaction = await connection.BeginTransactionAsync();

        await connection.ExecuteAsync(sql, parameters, transaction);
        await transaction.CommitAsync();
    }

    /// <summary>
    /// Převede datum z databáze na DateOnly.
    /// </summary>
    private static DateOnly? ToDateOnly(DateTime? value)
    {
        if (value is null)
        {
            return null;
        }

        return DateOnly.FromDateTime(value.Value);
    }

    /// <summary>
    /// Převede řádek sloupce na DTO.
    /// </summary>
    private static KanbanBoardColumnDto ToColumn(ColumnRow row)
    {
        return new KanbanBoardColumnDto
        {
            Id = row.Id,
            ProjectId = row.ProjectId,
            Title = row.Title,
            IsDone = row.IsDone,
        };
    }

    /// <summary>
    /// Převede řádek úkolu na DTO.
    /// </summary>
    private static KanbanBoardTaskDto ToTask(TaskRow row)
    {
        return new KanbanBoardTaskDto
        {
            Id = row.Id,
            ProjectId = row.ProjectId,
            ColumnId = row.ColumnId,
            Position = row.Position,
            Title = row.Title,
            Priority = row.Priority,
            Assignee = row.Assignee,
            AssigneeId = row.AssigneeId,
            DueDate = ToDateOnly(row.DueDate),
        };
    }

    /// <summary>
    /// Řádek tabulky project.
    /// </summary>
    private sealed class ProjectRow
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string TeamName { get; set; } = string.Empty;

        public DateTime? DueDate { get; set; }
    }

    /// <summary>
    /// Řádek tabulky board_column.
    /// </summary>
    private sealed class ColumnRow
    {
        public int Id { get; set; }

        public int ProjectId { get; set; }

        public string Title { get; set; } = string.Empty;

        public bool IsDone { get; set; }
    }

    /// <summary>
    /// Řádek tabulky task se jménem řešitele.
    /// </summary>
    private sealed class TaskRow
    {
        public int Id { get; set; }

        public int ProjectId { get; set; }

        public int ColumnId { get; set; }

        public int Position { get; set; }

        public string Title { get; set; } = string.Empty;

        public string? Priority { get; set; }

        public string Assignee { get; set; } = string.Empty;

        public long AssigneeId { get; set; }

        public DateTime? DueDate { get; set; }
    }
}
