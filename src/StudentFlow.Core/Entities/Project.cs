namespace StudentFlow.Core.Entities;

/// <summary>
/// Studentský týmový projekt.
/// </summary>
public class Project
{
    /// <summary>
    /// Identifikátor projektu. Vyplní ho úložiště po uložení.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Název projektu.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Popis projektu nebo null, pokud není vyplněn.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Plánovaný termín odevzdání projektu.
    /// </summary>
    public DateOnly Deadline { get; set; }

    /// <summary>
    /// Tým, který na projektu pracuje.
    /// </summary>
    public Team Team { get; set; } = new();
}
