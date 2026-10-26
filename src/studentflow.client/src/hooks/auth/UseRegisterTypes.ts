import type { FormEvent } from 'react'
import type { RegisterErrors, RegisterFormValues } from '../../models/auth/AuthTypes'

/// <summary>
/// Stav odesílání registrace.
/// </summary>
export type RegisterStatus = 'idle' | 'submitting' | 'error'

/// <summary>
/// Vstupní parametry hooku registrace.
/// </summary>
export interface UseRegisterOptions {
  /// <summary>
  /// Zavolá se po úspěšné registraci.
  /// </summary>
  onRegistered: () => void
}

/// <summary>
/// Data a operace formuláře registrace.
/// </summary>
export interface UseRegisterResult {
  /// <summary>
  /// Aktuální hodnoty formuláře.
  /// </summary>
  values: RegisterFormValues

  /// <summary>
  /// Chybové hlášky polí.
  /// </summary>
  errors: RegisterErrors

  /// <summary>
  /// Stav odesílání.
  /// </summary>
  status: RegisterStatus

  /// <summary>
  /// Obecná chyba registrace.
  /// </summary>
  submitError: string | null

  /// <summary>
  /// Změní hodnotu pole a smaže jeho chybu.
  /// </summary>
  setValue: <K extends keyof RegisterFormValues>(field: K, value: RegisterFormValues[K]) => void

  /// <summary>
  /// Ověří a odešle formulář.
  /// </summary>
  submit: (event: FormEvent<HTMLFormElement>) => Promise<void>
}
