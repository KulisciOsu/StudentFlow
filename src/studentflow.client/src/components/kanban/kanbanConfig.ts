/// <summary>
/// Nastavení Kanban nástěnky.
/// </summary>
export const kanbanConfig = {
  /// <summary>
  /// Identifikátor zobrazeného projektu.
  /// </summary>
  projectId: 1,

  /// <summary>
  /// Media query pro mobilní zobrazení se záložkami.
  /// </summary>
  mobileQuery: '(max-width: 600px)',

  /// <summary>
  /// Nastavení animací.
  /// </summary>
  animation: {
    /// <summary>
    /// Délka animace v milisekundách.
    /// </summary>
    duration: 200,

    /// <summary>
    /// Průběh animace.
    /// </summary>
    easing: 'ease',
  },

  /// <summary>
  /// Nastavení přetahování.
  /// </summary>
  drag: {
    /// <summary>
    /// Vzdálenost pro zahájení přesunu v pixelech.
    /// </summary>
    activationDistance: 6,

    /// <summary>
    /// Doba podržení prstu pro zahájení přesunu na mobilu v milisekundách.
    /// </summary>
    touchDelay: 200,
  },
} as const
