import type { Project } from '../../models/projects/ProjectTypes'

/// <summary>
/// Stav načítání projektu.
/// </summary>
export type ProjectLoadingStatus = 'loading' | 'ready' | 'error' | 'forbidden'

/// <summary>
/// Data a stav projektu nástěnky.
/// </summary>
export interface KanbanProjectState {
  /// <summary>
  /// Načtený projekt.
  /// </summary>
  project: Project | null

  /// <summary>
  /// Stav načítání.
  /// </summary>
  status: ProjectLoadingStatus
}
