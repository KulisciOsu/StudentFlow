namespace StudentFlow.Api.Models;

/// <summary>
/// DTO požadavku na přesun úkolu.
/// </summary>
public class MoveTaskRequestDto
{
    /// <summary>
    /// Identifikátor cílového sloupce.
    /// </summary>
    public required int ColumnId { get; init; }

    /// <summary>
    /// Cílová pozice ve sloupci od nuly.
    /// </summary>
    public required int Position { get; init; }
}
