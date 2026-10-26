/// <summary>
/// Nastavení formuláře projektu.
/// </summary>
export const projectConfig = {
  /// <summary>
  /// Nejvyšší povolené délky textů. Musí odpovídat validaci na backendu.
  /// </summary>
  maxLength: {
    /// <summary>
    /// Název projektu.
    /// </summary>
    name: 100,

    /// <summary>
    /// Popis projektu.
    /// </summary>
    description: 1000,

    /// <summary>
    /// Název týmu.
    /// </summary>
    teamName: 100,
  },
} as const
