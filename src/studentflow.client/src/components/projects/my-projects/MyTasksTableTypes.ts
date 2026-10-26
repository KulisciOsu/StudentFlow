import type { MyTask } from '../../../models/projects/MyProjectsTypes'

/// <summary>
/// Vstupní parametry tabulky úkolů studenta.
/// </summary>
export interface MyTasksTableProps {
  /// <summary>
  /// Úkoly studenta napříč projekty.
  /// </summary>
  tasks: MyTask[]

  /// <summary>
  /// Otevře detail úkolu.
  /// </summary>
  onOpenTask: (task: MyTask) => void
}

/// <summary>
/// Filtr tabulky úkolů.
/// </summary>
export type MyTasksFilter = 'open' | 'done'
