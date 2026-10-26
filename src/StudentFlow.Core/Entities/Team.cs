namespace StudentFlow.Core.Entities;

/// <summary>
/// Tým, který pracuje na studentském projektu.
/// </summary>
public class Team
{
    /// <summary>
    /// Identifikátor týmu. Vyplní ho úložiště po uložení.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Název týmu.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Členové týmu včetně jeho vedoucího.
    /// </summary>
    public List<TeamMember> Members { get; set; } = [];
}
