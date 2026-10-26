import type { AuthUser } from '../../../models/auth/AuthTypes'

/// <summary>
/// Vstupní parametry obrazovky Přihlášení.
/// </summary>
export interface LoginPageProps {
  /// <summary>
  /// Zpráva nad formulářem, například po registraci.
  /// </summary>
  notice?: string

  /// <summary>
  /// Zavolá se po úspěšném přihlášení.
  /// </summary>
  onLoggedIn: (user: AuthUser) => void

  /// <summary>
  /// Přejde na Registraci.
  /// </summary>
  onGoToRegister: () => void
}
