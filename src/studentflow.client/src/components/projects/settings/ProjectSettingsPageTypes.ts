import type { HomeLink, NavigateTo } from '../../../models/navigation/NavigationTypes'

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

  /// <summary>
  /// Přehled, na který vede drobečková navigace.
  /// </summary>
  home: HomeLink

  /// <summary>
  /// Přechod na jinou stránku aplikace.
  /// </summary>
  onNavigate: NavigateTo
}
