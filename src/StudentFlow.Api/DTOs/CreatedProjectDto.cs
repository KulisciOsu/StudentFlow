namespace StudentFlow.Api.DTOs;

/// <summary>
/// Odpověď po založení projektu.
/// </summary>
public class CreatedProjectDto
{
    /// <summary>
    /// Identifikátor založeného projektu.
    /// </summary>
    public required int Id { get; init; }

    /// <summary>
    /// Název založeného projektu.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Identifikátor týmu založeného spolu s projektem.
    /// </summary>
    public required int TeamId { get; init; }
}
