import type { Column } from '../../../models/ColumnTypes'

/// <summary>
/// Vstupní parametry sloupce.
/// </summary>
export interface KanbanColumnProps {
  /// <summary>
  /// Data sloupce.
  /// </summary>
  column: Column
}
