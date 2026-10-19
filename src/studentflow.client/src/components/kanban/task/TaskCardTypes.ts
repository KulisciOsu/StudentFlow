import type { Task } from '../../../models/tasks/TaskTypes'

/// <summary>
/// Vstupní parametry karty úkolu.
/// </summary>
export interface TaskCardProps {
  /// <summary>
  /// Data úkolu.
  /// </summary>
  task: Task
}
