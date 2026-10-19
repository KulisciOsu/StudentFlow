import type { Column } from '../../../models/columns/ColumnTypes'
import type { Task } from '../../../models/tasks/TaskTypes'

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
