/// <summary>
/// Definice sloupce Kanban nástěnky se stabilním identifikátorem a názvem.
/// </summary>
export interface Column {
  /// <summary>
  /// Stabilní identifikátor sloupce nezávislý na jeho zobrazovaném názvu.
  /// </summary>
  id: string

  /// <summary>
  /// Název zobrazovaný v záhlaví sloupce.
  /// </summary>
  title: string
}
