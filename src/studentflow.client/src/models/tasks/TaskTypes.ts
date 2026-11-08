/// <summary>
/// Priorita úkolu.
/// </summary>
export type TaskPriority = 'low' | 'medium' | 'high'

/// <summary>
/// Úkol nástěnky.
/// </summary>
export interface Task {
  /// <summary>
  /// Identifikátor úkolu.
  /// </summary>
  id: number

  /// <summary>
  /// Název úkolu.
  /// </summary>
  title: string

  /// <summary>
  /// Identifikátor sloupce.
  /// </summary>
  columnId: number

  /// <summary>
  /// Pozice úkolu ve sloupci od nuly.
  /// </summary>
  position: number

  /// <summary>
  /// Priorita úkolu, pokud je stanovena.
  /// </summary>
  priority: TaskPriority | null

  /// <summary>
  /// Jméno řešitele.
  /// </summary>
  assignee: string

  /// <summary>
  /// Termín ve formátu RRRR-MM-DD, pokud je stanoven.
  /// </summary>
  dueDate: string | null
}
