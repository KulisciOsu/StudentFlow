import type { Column } from '../../../models/ColumnTypes'
import type { Task } from '../../../models/TaskTypes'

/// <summary>
/// Vstupní parametry sloupce.
/// </summary>
export interface KanbanColumnProps {
  /// <summary>
  /// Data sloupce.
  /// </summary>
  column: Column

  /// <summary>
  /// Úkoly sloupce.
  /// </summary>
  tasks: Task[]
}
