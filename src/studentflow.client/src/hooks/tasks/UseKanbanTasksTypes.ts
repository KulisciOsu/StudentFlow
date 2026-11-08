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
/// Přesun úkolu, který se nepodařilo uložit.
/// </summary>
export interface FailedTaskMove {
  /// <summary>
  /// Úkol v původním stavu před přesunem.
  /// </summary>
  task: Task

  /// <summary>
  /// Cílový sloupec.
  /// </summary>
  columnId: number

  /// <summary>
  /// Cílová pozice.
  /// </summary>
  position: number
}

/// <summary>
/// Data a operace úkolů nástěnky.
/// </summary>
export interface KanbanTasksResult extends KanbanTasksState {
  /// <summary>
  /// Poslední neúspěšný přesun, nebo null.
  /// </summary>
  moveError: FailedTaskMove | null

  /// <summary>
  /// Přesune úkol a uloží změnu.
  /// </summary>
  moveTask: (taskId: number, columnId: number, position: number) => Promise<void>

  /// <summary>
  /// Zopakuje neúspěšný přesun.
  /// </summary>
  retryMove: () => void

  /// <summary>
  /// Skryje hlášku o neúspěšném přesunu.
  /// </summary>
  dismissMoveError: () => void
}
