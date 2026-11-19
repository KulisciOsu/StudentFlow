
namespace StudentFlow.Core.Projects;

/// <summary>
/// Obsahuje údaje potřebné pro založení nového projektu.
/// </summary>
public sealed class CreateProjectCommand
{
    /// <summary>
    /// Název nového projektu.
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Popis nového projektu.
    /// </summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>
    /// Název týmu, kterému projekt patří.
    /// </summary>
    public string TeamName { get; init; } = string.Empty;

    /// <summary>
    /// Termín dokončení projektu.
    /// </summary>
    public DateOnly? DueDate { get; init; }
}
