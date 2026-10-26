import type { MyProjectsData } from '../../models/projects/MyProjectsTypes'

/// <summary>
/// Stav načítání rozcestníku.
/// </summary>
export type MyProjectsLoadingStatus = 'loading' | 'ready' | 'error'

/// <summary>
/// Data a stav rozcestníku studenta.
/// </summary>
export interface MyProjectsResult extends MyProjectsData {
  /// <summary>
  /// Stav načítání.
  /// </summary>
  status: MyProjectsLoadingStatus
}
