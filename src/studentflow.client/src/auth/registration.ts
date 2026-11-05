import { authPost, responseFailure } from './client'
import { isRecord, isAuthUser } from './types'
import type { AccountType, AuthUser } from './types'
export { AuthFailure as RegistrationFailure } from './client'
export type { AccountType } from './types'
/** Stav formuláře před validací, včetně dosud nevybraného typu účtu. */
export type RegistrationFields = {
  displayName: string
  email: string
  password: string
  /** Slouží jen pro kontrolu shody ve formuláři; do API se neposílá. */
  passwordConfirmation: string
  accountType: AccountType | ''
}
export type RegistrationErrors = Partial<Record<keyof RegistrationFields, string>>
export type RegisteredUser = AuthUser

/** Ověří údaje a shodu hesel pro UI; autoritativní kontrolu registrace provádí také server. */
export function validateRegistration(fields: RegistrationFields): RegistrationErrors {
  const errors: RegistrationErrors = {}
  const nameLength = Array.from(fields.displayName.trim()).length
  if (nameLength < 1 || nameLength > 160) errors.displayName = 'Zadejte jméno a příjmení o délce 1 až 160 znaků.'
  const email = fields.email.trim()
  if (Array.from(email).length > 320 || !/^[^\s@]+@[^\s@]+$/u.test(email)) errors.email = 'Zadejte platnou e-mailovou adresu.'
  const passwordLength = Array.from(fields.password).length
  if (passwordLength < 15 || passwordLength > 128) errors.password = 'Heslo musí mít 15 až 128 znaků.'
  if (fields.password !== fields.passwordConfirmation) errors.passwordConfirmation = 'Hesla se neshodují.'
  if (fields.accountType !== 'STUDENT' && fields.accountType !== 'TEACHER') errors.accountType = 'Vyberte typ účtu: Člen, nebo Vyučující.'
  return errors
}

/** Založí účet a ověří odpověď 201; session nevzniká a uživatel se musí následně přihlásit. */
export async function registerAccount(fields: RegistrationFields, signal?: AbortSignal): Promise<RegisteredUser> {
  const payload = { displayName: fields.displayName, email: fields.email, password: fields.password, accountType: fields.accountType }
  const response = await authPost('/api/auth/register', payload, signal)
  if (response.status === 201) {
    const value: unknown = await response.clone().json().catch(() => null)
    if (isRecord(value) && isAuthUser(value.user)) return value.user
  }
  throw await responseFailure(response, 'Vytvoření účtu se nepodařilo potvrdit. Zkuste to později.')
}
