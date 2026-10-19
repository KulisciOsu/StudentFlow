import type { Column } from '../../../models/ColumnTypes'

/// <summary>
/// Definice sloupce předaná komponentě pro vykreslení.
/// </summary>
export interface KanbanColumnProps {
  /// <summary>
  /// Definice sloupce, jehož záhlaví a plochu komponenta vykreslí.
  /// </summary>
  column: Column
}
