/// <summary>
/// Vlastnosti stránky „Nastavení projektu“.
/// </summary>
export interface ProjectSettingsPageProps {
  /// <summary>
  /// Identifikátor projektu z adresy /projects/{projectId}/settings.
  /// </summary>
  projectId: number

  /// <summary>
  /// ID přihlášeného uživatele ze session; podle něj stránka pozná vedoucího.
  /// </summary>
  currentUserId: string
}
