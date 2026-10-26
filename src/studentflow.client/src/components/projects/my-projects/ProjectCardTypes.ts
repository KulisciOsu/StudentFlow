import type { ProjectSummary } from '../../../models/projects/MyProjectsTypes'

/// <summary>
/// Vstupní parametry karty projektu.
/// </summary>
export interface ProjectCardProps {
  /// <summary>
  /// Data projektu.
  /// </summary>
  project: ProjectSummary

  /// <summary>
  /// Otevře projekt.
  /// </summary>
  onOpen: (projectId: number) => void
}
