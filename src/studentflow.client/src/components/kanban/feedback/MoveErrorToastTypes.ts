/// <summary>
/// Vstupní parametry hlášky o neúspěšném přesunu.
/// </summary>
export interface MoveErrorToastProps {
  /// <summary>
  /// Název úkolu.
  /// </summary>
  taskTitle: string

  /// <summary>
  /// Název sloupce, do kterého se karta vrátila.
  /// </summary>
  columnTitle: string

  /// <summary>
  /// Zopakuje přesun.
  /// </summary>
  onRetry: () => void

  /// <summary>
  /// Skryje hlášku.
  /// </summary>
  onDismiss: () => void
}
