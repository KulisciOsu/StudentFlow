import type { Column } from '../../../models/columns/ColumnTypes'
import type { Task } from '../../../models/tasks/TaskTypes'

/// <summary>
/// Vstupní parametry mobilní nástěnky.
/// </summary>
export interface MobileBoardProps {
  /// <summary>
  /// Sloupce nástěnky.
  /// </summary>
  columns: Column[]

  /// <summary>
  /// Zobrazené úkoly po použití filtrů.
  /// </summary>
  tasks: Task[]

  /// <summary>
  /// Určuje, zda má uživatel nástěnku jen pro čtení.
  /// </summary>
  isReadOnly: boolean

  /// <summary>
  /// Přesune úkol na konec cílového sloupce.
  /// </summary>
  onMoveTask: (taskId: number, columnId: number) => void
}
