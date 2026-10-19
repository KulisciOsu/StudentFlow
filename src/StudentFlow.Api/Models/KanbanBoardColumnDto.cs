namespace StudentFlow.Api.Models;

/// <summary>
/// DTO sloupce nástěnky.
/// </summary>
public class KanbanBoardColumnDto
{
    /// <summary>
    /// Identifikátor sloupce.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Název sloupce.
    /// </summary>
    public required string Title { get; init; }
}
