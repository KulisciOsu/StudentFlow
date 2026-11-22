import type { HomeLink, NavigateTo } from '../../../models/navigation/NavigationTypes'
import type { Project } from '../../../models/projects/ProjectTypes'

/// <summary>
/// Vstupní parametry hlavičky nástěnky.
/// </summary>
export interface BoardHeaderProps {
  /// <summary>
  /// Projekt nástěnky.
  /// </summary>
  project: Project

  /// <summary>
  /// Přehled, na který vede drobečková navigace.
  /// </summary>
  home: HomeLink

  /// <summary>
  /// Přechod na jinou stránku aplikace.
  /// </summary>
  onNavigate: NavigateTo
}
