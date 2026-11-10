namespace StudentFlow.Core.Kanban;

/// <summary>
/// Role přihlášeného uživatele vůči projektu.
/// </summary>
public enum ProjectRoleEnum
{
    /// <summary>
    /// Uživatel nemá k projektu přístup.
    /// </summary>
    None,

    /// <summary>
    /// Člen týmu.
    /// </summary>
    Member,

    /// <summary>
    /// Vedoucí týmu.
    /// </summary>
    Leader,

    /// <summary>
    /// Vyučující.
    /// </summary>
    Teacher,
}
