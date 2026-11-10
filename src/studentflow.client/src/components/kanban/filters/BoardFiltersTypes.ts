import type { TaskFilters } from '../../../utils/tasks/filterTasks'

/// <summary>
/// Vstupní parametry filtrů nástěnky.
/// </summary>
export interface BoardFiltersProps {
  /// <summary>
  /// Aktuální filtry.
  /// </summary>
  filters: TaskFilters

  /// <summary>
  /// Jména řešitelů pro výběr.
  /// </summary>
  assignees: string[]

  /// <summary>
  /// Určuje, zda zobrazit filtr „Jen moje úkoly“.
  /// </summary>
  showOnlyMine: boolean

  /// <summary>
  /// Změní filtry.
  /// </summary>
  onChange: (filters: TaskFilters) => void
}
