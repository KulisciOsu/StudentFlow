namespace StudentFlow.Api.Models;

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
    /// Název úkolu.
    /// </summary>
    public required string Title { get; init; }

    /// <summary>
    /// Identifikátor sloupce.
    /// </summary>
    public required int ColumnId { get; init; }

    /// <summary>
    /// Pozice úkolu ve sloupci od nuly.
    /// </summary>
    public required int Position { get; init; }
}
