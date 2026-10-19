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

/// <summary>
/// Data a operace úkolů nástěnky.
/// </summary>
export interface KanbanTasksResult extends KanbanTasksState {
  /// <summary>
  /// Přesune úkol na danou pozici.
  /// </summary>
  moveTask: (taskId: number, columnId: number, position: number) => void
}
