using StudentFlow.Core.Kanban;

namespace StudentFlow.Data.Kanban;

/// <summary>
/// Přístup k datům nástěnky. Data jsou zatím v paměti serveru, v KAN-98 se napojí na databázi přes Dapper.
/// </summary>
public class KanbanBoardRepository : IKanbanBoardRepository
{
    private readonly object _lock = new();

    private readonly List<KanbanProjectDto> _projects =
    [
        new KanbanProjectDto { Id = 1, Name = "Rezervační systém knihovny", TeamName = "Tým Sova", DueDate = new DateOnly(2027, 1, 15) },
        new KanbanProjectDto { Id = 2, Name = "Evidence praxí", TeamName = "Tým Liška", DueDate = null },
    ];

    private readonly List<KanbanBoardColumnDto> _columns =
    [
        new KanbanBoardColumnDto { Id = 1, ProjectId = 1, Title = "K řešení", IsDone = false },
        new KanbanBoardColumnDto { Id = 2, ProjectId = 1, Title = "Rozpracováno", IsDone = false },
        new KanbanBoardColumnDto { Id = 3, ProjectId = 1, Title = "Ke kontrole", IsDone = false },
        new KanbanBoardColumnDto { Id = 4, ProjectId = 1, Title = "Hotovo", IsDone = true },
        new KanbanBoardColumnDto { Id = 5, ProjectId = 2, Title = "K řešení", IsDone = false },
    ];

    private List<KanbanBoardTaskDto> _tasks =
    [
        CreateTask(1, 1, 1, 0, "Testovací scénáře přihlášení", "medium", "Lucie Černá", new DateOnly(2026, 11, 12)),
        CreateTask(2, 1, 1, 1, "Formulář rezervace", "high", "Tomáš Dvořák", new DateOnly(2026, 11, 20)),
        CreateTask(3, 1, 1, 2, "Export výpůjček do CSV", "low", "Martin Kučera", new DateOnly(2026, 12, 4)),
        CreateTask(4, 1, 2, 0, "Návrh databázového modelu", "high", "Jan Novák", new DateOnly(2026, 10, 22)),
        CreateTask(5, 1, 2, 1, "API pro výpůjčky", "medium", "Martin Kučera", new DateOnly(2026, 11, 5)),
        CreateTask(6, 1, 2, 2, "Wireframy obrazovek", "medium", "Petra Svobodová", new DateOnly(2026, 11, 9)),
        CreateTask(7, 1, 3, 0, "Diagram případů užití", "medium", "Lucie Černá", new DateOnly(2026, 11, 6)),
        CreateTask(8, 1, 4, 0, "Vize projektu", null, "Petra Svobodová", new DateOnly(2026, 10, 10)),
        CreateTask(9, 1, 4, 1, "Popis problému", null, "Jan Novák", new DateOnly(2026, 10, 12)),
        CreateTask(10, 1, 4, 2, "Risk-list", null, "Lucie Černá", new DateOnly(2026, 10, 15)),
        CreateTask(11, 2, 5, 0, "Analýza požadavků", "medium", "Karel Malý", null),
    ];

    /// <inheritdoc />
    public Task<KanbanProjectDto?> GetProjectAsync(int projectId)
    {
        var project = _projects.FirstOrDefault(item => item.Id == projectId);

        return Task.FromResult(project);
    }

    /// <inheritdoc />
    public Task<List<KanbanBoardColumnDto>> GetColumnsAsync(int projectId)
    {
        var columns = _columns
            .Where(item => item.ProjectId == projectId)
            .ToList();

        return Task.FromResult(columns);
    }

    /// <inheritdoc />
    public Task<KanbanBoardColumnDto?> GetColumnAsync(int columnId)
    {
        var column = _columns.FirstOrDefault(item => item.Id == columnId);

        return Task.FromResult(column);
    }

    /// <inheritdoc />
    public Task<List<KanbanBoardTaskDto>> GetTasksAsync(int projectId)
    {
        lock (_lock)
        {
            var tasks = _tasks
                .Where(item => item.ProjectId == projectId)
                .ToList();

            return Task.FromResult(tasks);
        }
    }

    /// <inheritdoc />
    public Task<KanbanBoardTaskDto?> GetTaskAsync(int taskId)
    {
        lock (_lock)
        {
            var task = _tasks.FirstOrDefault(item => item.Id == taskId);

            return Task.FromResult(task);
        }
    }

    /// <inheritdoc />
    public Task SaveTasksAsync(int projectId, List<KanbanBoardTaskDto> tasks)
    {
        lock (_lock)
        {
            var otherProjects = _tasks
                .Where(item => item.ProjectId != projectId)
                .ToList();

            otherProjects.AddRange(tasks);
            _tasks = otherProjects;
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Vytvoří úkol pro výchozí data.
    /// </summary>
    private static KanbanBoardTaskDto CreateTask(int id, int projectId, int columnId, int position, string title, string? priority, string assignee, DateOnly? dueDate)
    {
        return new KanbanBoardTaskDto
        {
            Id = id,
            ProjectId = projectId,
            ColumnId = columnId,
            Position = position,
            Title = title,
            Priority = priority,
            Assignee = assignee,
            DueDate = dueDate,
        };
    }
}
