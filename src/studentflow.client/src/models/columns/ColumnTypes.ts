/// <summary>
/// Sloupec nástěnky.
/// </summary>
export interface Column {
  /// <summary>
  /// Identifikátor sloupce.
  /// </summary>
  id: number

  /// <summary>
  /// Název sloupce.
  /// </summary>
  title: string

  /// <summary>
  /// Určuje, zda sloupec obsahuje dokončené úkoly.
  /// </summary>
  isDone: boolean
}
