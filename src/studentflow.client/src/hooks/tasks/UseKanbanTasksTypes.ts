import type { Task } from '../../models/tasks/TaskTypes'

/// <summary>
/// Stav načítání úkolů.
/// </summary>
export type TasksLoadingStatus = 'loading' | 'ready' | 'error'

/// <summary>
/// Data a stav úkolů nástěnky.
/// </summary>
export interface KanbanTasksState {
  /// <summary>
  /// Načtené úkoly.
  /// </summary>
  tasks: Task[]

  /// <summary>
  /// Stav načítání.
  /// </summary>
  status: TasksLoadingStatus
}
