using StudentFlow.Core.Kanban;

namespace StudentFlow.Api.Tests;

/// <summary>
/// Náhradní repozitář s daty v paměti pro testy controllerů.
/// </summary>
public class FakeKanbanBoardRepository : IKanbanBoardRepository
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

    /// <summary>
    /// Členství (projekt, uživatel) a zda je uživatel vedoucí.
    /// </summary>
    private readonly Dictionary<(int ProjectId, long UserId), bool> _members = new()
    {
        [(1, 1)] = true,
        [(1, 2)] = false,
        [(1, 3)] = false,
        [(1, 4)] = false,
        [(1, 5)] = false,
        [(2, 6)] = true,
    };

    private List<KanbanBoardTaskDto> _tasks =
    [
        CreateTask(1, 1, 1, 0, "Testovací scénáře přihlášení", "medium", "Lucie Černá", 4, new DateOnly(2026, 11, 12)),
        CreateTask(2, 1, 1, 1, "Formulář rezervace", "high", "Tomáš Dvořák", 3, new DateOnly(2026, 11, 20)),
        CreateTask(3, 1, 1, 2, "Export výpůjček do CSV", "low", "Martin Kučera", 5, new DateOnly(2026, 12, 4)),
        CreateTask(4, 1, 2, 0, "Návrh databázového modelu", "high", "Jan Novák", 2, new DateOnly(2026, 10, 22)),
        CreateTask(5, 1, 2, 1, "API pro výpůjčky", "medium", "Martin Kučera", 5, new DateOnly(2026, 11, 5)),
        CreateTask(6, 1, 2, 2, "Wireframy obrazovek", "medium", "Petra Svobodová", 1, new DateOnly(2026, 11, 9)),
        CreateTask(7, 1, 3, 0, "Diagram případů užití", "medium", "Lucie Černá", 4, new DateOnly(2026, 11, 6)),
        CreateTask(8, 1, 4, 0, "Vize projektu", null, "Petra Svobodová", 1, new DateOnly(2026, 10, 10)),
        CreateTask(9, 1, 4, 1, "Popis problému", null, "Jan Novák", 2, new DateOnly(2026, 10, 12)),
        CreateTask(10, 1, 4, 2, "Risk-list", null, "Lucie Černá", 4, new DateOnly(2026, 10, 15)),
        CreateTask(11, 2, 5, 0, "Analýza požadavků", "medium", "Karel Malý", 6, null),
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
    public Task<bool?> GetMembershipAsync(int projectId, long userId)
    {
        if (_members.TryGetValue((projectId, userId), out var isLeader))
        {
            return Task.FromResult<bool?>(isLeader);
        }

        return Task.FromResult<bool?>(null);
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
    private static KanbanBoardTaskDto CreateTask(int id, int projectId, int columnId, int position, string title, string? priority, string assignee, long assigneeId, DateOnly? dueDate)
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
            AssigneeId = assigneeId,
            DueDate = dueDate,
        };
    }
}
