/// <summary>
/// Vstupní parametry obrazovky Registrace.
/// </summary>
export interface RegisterPageProps {
  /// <summary>
  /// Zavolá se po úspěšné registraci.
  /// </summary>
  onRegistered: () => void

  /// <summary>
  /// Přejde na Přihlášení.
  /// </summary>
  onGoToLogin: () => void
}
