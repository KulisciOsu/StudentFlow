/// <summary>
/// Nastavení Kanban nástěnky.
/// </summary>
export const kanbanConfig = {
  /// <summary>
  /// Media query pro mobilní zobrazení se záložkami.
  /// </summary>
  mobileQuery: '(max-width: 600px)',

  /// <summary>
  /// Nastavení animací.
/// </summary>
  animation: {
    duration: 500,
    easing: 'ease-in-out',
  },

  /// <summary>
  /// Nastavení přetahování.
  /// </summary>
  drag: {
    activationDistance: 6,
    touchDelay: 200,
  },
} as const