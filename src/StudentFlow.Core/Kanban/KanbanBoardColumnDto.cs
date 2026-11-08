namespace StudentFlow.Core.Kanban;

/// <summary>
/// DTO sloupce nástěnky.
/// </summary>
public class KanbanBoardColumnDto
{
    /// <summary>
    /// Identifikátor sloupce.
    /// </summary>
    public required int Id { get; init; }

    /// <summary>
    /// Identifikátor projektu.
    /// </summary>
    public required int ProjectId { get; init; }

    /// <summary>
    /// Název sloupce.
    /// </summary>
    public required string Title { get; init; }

    /// <summary>
    /// Určuje, zda sloupec obsahuje dokončené úkoly.
    /// </summary>
    public required bool IsDone { get; init; }
}
