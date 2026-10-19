/// <summary>
/// Nastavení Kanban nástěnky.
/// </summary>
export const kanbanConfig = {
  /// <summary>
  /// Nastavení animací.
  /// </summary>
  animation: {
    /// <summary>
    /// Délka animace v milisekundách.
    /// </summary>
    duration: 500,

    /// <summary>
    /// Průběh animace.
    /// </summary>
    easing: 'ease-in-out',
  },

  /// <summary>
  /// Nastavení přetahování.
  /// </summary>
  drag: {
    /// <summary>
    /// Vzdálenost pro zahájení přesunu v pixelech.
    /// </summary>
    activationDistance: 6,
  },
} as const
