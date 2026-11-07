
namespace StudentFlow.Core.Dashboard;

/// <summary>
/// Datová vrstva pro dashboard přihlášeného uživatele.
/// </summary>
public interface IDashboardRepository
{
    Task<DashboardData> GetDashboardAsync(
        long userId,
        CancellationToken cancellationToken = default);
}

public sealed class DashboardData
{
    public List<DashboardProjectData> Projects { get; set; } = [];
    public List<DashboardTaskData> Tasks { get; set; } = [];
}

public sealed class DashboardProjectData
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Team { get; set; } = "";
    public int MembersCount { get; set; }
    public string Role { get; set; } = "";
    public int Completed { get; set; }
    public int Total { get; set; }
    public int OpenTasks { get; set; }
    public int OverdueTasks { get; set; }
    public string Milestone { get; set; } = "";
    public List<string> Members { get; set; } = [];
}

public sealed class DashboardTaskData
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Project { get; set; } = "";
    public string Deadline { get; set; } = "";
    public string Priority { get; set; } = "";
    public string Status { get; set; } = "";
    public bool Overdue { get; set; }
    public bool Completed { get; set; }
}
