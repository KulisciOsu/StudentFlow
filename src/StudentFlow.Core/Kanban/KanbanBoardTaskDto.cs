namespace StudentFlow.Core.Kanban;

/// <summary>
/// DTO úkolu nástěnky.
/// </summary>
public class KanbanBoardTaskDto
{
    /// <summary>
    /// Identifikátor úkolu.
    /// </summary>
    public required int Id { get; init; }

    /// <summary>
    /// Identifikátor projektu.
    /// </summary>
    public required int ProjectId { get; init; }

    /// <summary>
    /// Identifikátor sloupce.
    /// </summary>
    public required int ColumnId { get; init; }

    /// <summary>
    /// Pozice úkolu ve sloupci od nuly.
    /// </summary>
    public required int Position { get; init; }

    /// <summary>
    /// Název úkolu.
    /// </summary>
    public required string Title { get; init; }

    /// <summary>
    /// Priorita: low, medium, high, nebo null.
    /// </summary>
    public string? Priority { get; init; }

    /// <summary>
    /// Jméno řešitele.
    /// </summary>
    public required string Assignee { get; init; }

    /// <summary>
    /// Termín, pokud je stanoven.
    /// </summary>
    public DateOnly? DueDate { get; init; }
}
