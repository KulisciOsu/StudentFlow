/// <summary>
/// Projekt nástěnky.
/// </summary>
export interface Project {
  /// <summary>
  /// Identifikátor projektu.
  /// </summary>
  id: number

  /// <summary>
  /// Název projektu.
  /// </summary>
  name: string

  /// <summary>
  /// Název týmu.
  /// </summary>
  teamName: string

  /// <summary>
  /// Termín odevzdání ve formátu RRRR-MM-DD, pokud je stanoven.
  /// </summary>
  dueDate: string | null

  /// <summary>
  /// Určuje, zda má přihlášený uživatel nástěnku jen pro čtení.
  /// </summary>
  isReadOnly: boolean
}
