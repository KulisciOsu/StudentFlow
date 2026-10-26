/// <summary>
/// Typ uživatelského účtu.
/// </summary>
export type AccountType = 'student' | 'teacher'

/// <summary>
/// Přihlášený uživatel.
/// </summary>
export interface AuthUser {
  /// <summary>
  /// Identifikátor uživatele.
  /// </summary>
  id: number

  /// <summary>
  /// Jméno a příjmení.
  /// </summary>
  name: string

  /// <summary>
  /// E-mail uživatele.
  /// </summary>
  email: string

  /// <summary>
  /// Typ účtu.
  /// </summary>
  accountType: AccountType
}

/// <summary>
/// Požadavek na přihlášení.
/// </summary>
export interface LoginRequest {
  /// <summary>
  /// E-mail uživatele.
  /// </summary>
  email: string

  /// <summary>
  /// Heslo uživatele.
  /// </summary>
  password: string

  /// <summary>
  /// Zůstat přihlášen.
  /// </summary>
  rememberMe: boolean
}

/// <summary>
/// Požadavek na registraci.
/// </summary>
export interface RegisterRequest {
  /// <summary>
  /// Jméno a příjmení.
  /// </summary>
  name: string

  /// <summary>
  /// E-mail uživatele.
  /// </summary>
  email: string

  /// <summary>
  /// Heslo uživatele.
  /// </summary>
  password: string

  /// <summary>
  /// Typ účtu.
  /// </summary>
  accountType: AccountType
}

/// <summary>
/// Hodnoty formuláře přihlášení.
/// </summary>
export interface LoginFormValues {
  email: string
  password: string
  rememberMe: boolean
}

/// <summary>
/// Hodnoty formuláře registrace.
/// </summary>
export interface RegisterFormValues {
  name: string
  email: string
  password: string
  passwordConfirm: string
  accountType: AccountType
}

/// <summary>
/// Chybové hlášky formuláře přihlášení.
/// </summary>
export type LoginErrors = Partial<Record<'email' | 'password', string>>

/// <summary>
/// Chybové hlášky formuláře registrace.
/// </summary>
export type RegisterErrors = Partial<Record<'name' | 'email' | 'password' | 'passwordConfirm', string>>
