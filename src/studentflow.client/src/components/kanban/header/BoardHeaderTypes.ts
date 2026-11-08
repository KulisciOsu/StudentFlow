import type { Project } from '../../../models/projects/ProjectTypes'

/// <summary>
/// Vstupní parametry hlavičky nástěnky.
/// </summary>
export interface BoardHeaderProps {
  /// <summary>
  /// Projekt nástěnky.
  /// </summary>
  project: Project
}
