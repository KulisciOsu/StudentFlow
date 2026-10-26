import type {
  LoginErrors, LoginFormValues, RegisterErrors, RegisterFormValues,
} from '../../models/auth/AuthTypes'

/// <summary>
/// Nejmenší povolená délka hesla.
/// </summary>
export const minPasswordLength = 8

/// <summary>
/// Ověří základní tvar e-mailové adresy.
/// </summary>
function isEmail(value: string): boolean {
  return /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(value)
}

/// <summary>
/// Ověří údaje formuláře přihlášení.
/// </summary>
export function validateLogin(values: LoginFormValues): LoginErrors {
  const errors: LoginErrors = {}

  if (values.email.trim().length === 0) errors.email = 'Zadejte e-mail.'
  if (values.password.length === 0) errors.password = 'Zadejte heslo.'

  return errors
}

/// <summary>
/// Ověří údaje formuláře registrace.
/// </summary>
export function validateRegister(values: RegisterFormValues): RegisterErrors {
  const errors: RegisterErrors = {}

  if (values.name.trim().length === 0) errors.name = 'Zadejte jméno a příjmení.'

  if (values.email.trim().length === 0) errors.email = 'Zadejte e-mail.'
  else if (!isEmail(values.email.trim())) errors.email = 'Zadejte platný e-mail.'

  if (values.password.length < minPasswordLength) {
    errors.password = `Heslo musí mít alespoň ${minPasswordLength} znaků.`
  }

  if (values.passwordConfirm !== values.password) errors.passwordConfirm = 'Hesla se neshodují.'

  return errors
}
