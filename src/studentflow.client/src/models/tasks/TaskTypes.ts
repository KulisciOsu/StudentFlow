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
}
