import type { Column } from '../../models/columns/ColumnTypes'

/// <summary>
/// Stav načítání sloupců.
/// </summary>
export type ColumnsLoadingStatus = 'loading' | 'ready' | 'error'

/// <summary>
/// Data a stav sloupců nástěnky.
/// </summary>
export interface KanbanColumnsState {
  /// <summary>
  /// Načtené sloupce.
  /// </summary>
  columns: Column[]

  /// <summary>
  /// Stav načítání.
  /// </summary>
  status: ColumnsLoadingStatus
}
