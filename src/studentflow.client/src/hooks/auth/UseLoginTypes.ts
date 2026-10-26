import type { FormEvent } from 'react'
import type { AuthUser, LoginErrors, LoginFormValues } from '../../models/auth/AuthTypes'

/// <summary>
/// Stav odesílání přihlášení.
/// </summary>
export type LoginStatus = 'idle' | 'submitting' | 'error'

/// <summary>
/// Vstupní parametry hooku přihlášení.
/// </summary>
export interface UseLoginOptions {
  /// <summary>
  /// Zavolá se po úspěšném přihlášení.
  /// </summary>
  onLoggedIn: (user: AuthUser) => void
}

/// <summary>
/// Data a operace formuláře přihlášení.
/// </summary>
export interface UseLoginResult {
  /// <summary>
  /// Aktuální hodnoty formuláře.
  /// </summary>
  values: LoginFormValues

  /// <summary>
  /// Chybové hlášky polí.
  /// </summary>
  errors: LoginErrors

  /// <summary>
  /// Stav odesílání.
  /// </summary>
  status: LoginStatus

  /// <summary>
  /// Obecná chyba přihlášení.
  /// </summary>
  submitError: string | null

  /// <summary>
  /// Změní hodnotu pole a smaže jeho chybu.
  /// </summary>
  setValue: <K extends keyof LoginFormValues>(field: K, value: LoginFormValues[K]) => void

  /// <summary>
  /// Ověří a odešle formulář.
  /// </summary>
  submit: (event: FormEvent<HTMLFormElement>) => Promise<void>
}
