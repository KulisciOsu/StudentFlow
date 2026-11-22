
namespace StudentFlow.Core.Kanban;

/// <summary>
/// DTO projektu nástěnky.
/// </summary>
public class KanbanProjectDto
{
    /// <summary>
    /// Identifikátor projektu.
    /// </summary>
    public required int Id { get; init; }

    /// <summary>
    /// Název projektu.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Popis projektu.
    /// </summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>
    /// Název týmu.
    /// </summary>
    public required string TeamName { get; init; }

    /// <summary>
    /// Termín odevzdání, pokud je stanoven.
    /// </summary>
    public DateOnly? DueDate { get; init; }

    /// <summary>
    /// Zda má přihlášený uživatel nástěnku jen pro čtení. Doplňuje API.
    /// </summary>
    public bool IsReadOnly { get; set; }

    /// <summary>
    /// Zda je přihlášený uživatel vedoucím týmu a smí otevřít nastavení projektu. Doplňuje API.
    /// </summary>
    public bool IsLeader { get; set; }
}
