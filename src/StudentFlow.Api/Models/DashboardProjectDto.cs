
namespace StudentFlow.Api.Models;

/// <summary>
/// Přehled projektu zobrazovaného na úvodním dashboardu.
/// </summary>
public sealed class DashboardProjectDto
{
    /// <summary>Identifikátor projektu.</summary>
    public int Id { get; set; }

    /// <summary>Název projektu.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Název týmu.</summary>
    public string Team { get; set; } = string.Empty;

    /// <summary>Počet členů týmu.</summary>
    public int MembersCount { get; set; }

    /// <summary>Role uživatele v projektu.</summary>
    public string Role { get; set; } = string.Empty;

    /// <summary>Počet dokončených úkolů.</summary>
    public int Completed { get; set; }

    /// <summary>Celkový počet úkolů.</summary>
    public int Total { get; set; }

    /// <summary>Počet otevřených úkolů uživatele.</summary>
    public int OpenTasks { get; set; }

    /// <summary>Počet úkolů po termínu.</summary>
    public int OverdueTasks { get; set; }

    /// <summary>Nejbližší milník projektu.</summary>
    public string Milestone { get; set; } = string.Empty;

    /// <summary>Iniciály členů týmu.</summary>
    public List<string> Members { get; set; } = [];
}
