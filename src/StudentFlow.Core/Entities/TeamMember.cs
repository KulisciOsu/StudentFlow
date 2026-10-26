using StudentFlow.Core.Enums;

namespace StudentFlow.Core.Entities;

/// <summary>
/// Členství uživatele v týmu.
/// </summary>
public class TeamMember
{
    /// <summary>
    /// Identifikátor uživatele, který je členem týmu.
    /// </summary>
    public int UserId { get; set; }

    /// <summary>
    /// Role uživatele v týmu.
    /// </summary>
    public TeamRole Role { get; set; }
}
