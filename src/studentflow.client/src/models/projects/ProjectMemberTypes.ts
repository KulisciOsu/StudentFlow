/// <summary>
/// Aktuální člen projektu, jak ho vrací API členů.
/// </summary>
export interface ProjectMember {
  /// <summary>
  /// Identifikátor uživatele jako text, aby se velká čísla v JavaScriptu nezaokrouhlila.
  /// </summary>
  userId: string

  /// <summary>
  /// Zobrazované jméno člena.
  /// </summary>
  displayName: string

  /// <summary>
  /// E-mail účtu člena.
  /// </summary>
  email: string

  /// <summary>
  /// Zda je člen vedoucím týmu; projekt má právě jednoho vedoucího.
  /// </summary>
  isLeader: boolean

  /// <summary>
  /// Okamžik přidání do projektu ve formátu ISO 8601.
  /// </summary>
  addedAt: string
}

/// <summary>
/// Seznam členů projektu s názvem týmu; vedoucí je první, ostatní podle jména.
/// </summary>
export interface ProjectMembers {
  /// <summary>
  /// Název týmu zobrazený pod nadpisem sekce.
  /// </summary>
  teamName: string

  /// <summary>
  /// Aktuální členové projektu.
  /// </summary>
  members: ProjectMember[]
}
