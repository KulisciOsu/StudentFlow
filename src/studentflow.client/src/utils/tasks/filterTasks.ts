import type { Task, TaskPriority } from '../../models/tasks/TaskTypes'

/// <summary>
/// Filtry nástěnky.
/// </summary>
export interface TaskFilters {
  /// <summary>
  /// Hledaný text v názvu úkolu.
  /// </summary>
  query: string

  /// <summary>
  /// Jméno řešitele, nebo prázdný text pro všechny.
  /// </summary>
  assignee: string

  /// <summary>
  /// Priorita, nebo null pro všechny.
  /// </summary>
  priority: TaskPriority | null
}

/// <summary>
/// Filtry bez omezení.
/// </summary>
export const emptyTaskFilters: TaskFilters = { query: '', assignee: '', priority: null }

/// <summary>
/// Převede text na malá písmena bez diakritiky.
/// </summary>
function normalize(text: string): string {
  return text.normalize('NFD').replace(/\p{Diacritic}/gu, '').toLowerCase().trim()
}

/// <summary>
/// Vrátí úkoly odpovídající filtrům.
/// </summary>
export function filterTasks(tasks: Task[], filters: TaskFilters): Task[] {
  const query = normalize(filters.query)

  return tasks.filter((task) => (!query || normalize(task.title).includes(query))
    && (!filters.assignee || task.assignee === filters.assignee)
    && (!filters.priority || task.priority === filters.priority))
}
