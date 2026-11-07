
namespace StudentFlow.Api.Models;

/// <summary>
/// Úkol uživatele zobrazovaný na úvodním dashboardu.
/// </summary>
public sealed class DashboardTaskDto
{
    /// <summary>Identifikátor úkolu.</summary>
    public int Id { get; set; }

    /// <summary>Název úkolu.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Název projektu, do kterého úkol patří.</summary>
    public string Project { get; set; } = string.Empty;

    /// <summary>Termín dokončení úkolu.</summary>
    public string Deadline { get; set; } = string.Empty;

    /// <summary>Priorita úkolu.</summary>
    public string Priority { get; set; } = string.Empty;

    /// <summary>Aktuální stav úkolu.</summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>Určuje, zda je úkol po termínu.</summary>
    public bool Overdue { get; set; }

    /// <summary>Určuje, zda je úkol dokončený.</summary>
    public bool Completed { get; set; }
}
