import { authPost, responseFailure } from './client'
import { registrationPasswordError } from './password'
import { registrationNameError } from './name'
import { privacyPolicyVersion } from './privacy'
import { isRecord, isAuthUser } from './types'
import type { AccountType, AuthUser } from './types'
export { AuthFailure as RegistrationFailure } from './client'
export type { AccountType } from './types'
/** Stav formuláře před validací, včetně dosud nevybraného typu účtu. */
export type RegistrationFields = {
  firstName: string
  lastName: string
  email: string
  password: string
  /** Slouží jen pro kontrolu shody ve formuláři; do API se neposílá. */
  passwordConfirmation: string
  accountType: AccountType | ''
  privacyAccepted: boolean
}
export type RegistrationErrors = Partial<Record<keyof RegistrationFields, string>>
export type RegisteredUser = AuthUser

/** Ověří údaje a shodu hesel pro UI; autoritativní kontrolu registrace provádí také server. */
export function validateRegistration(fields: RegistrationFields): RegistrationErrors {
  const errors: RegistrationErrors = {}
  const firstNameError = registrationNameError(fields.firstName, 'Jméno')
  if (firstNameError) errors.firstName = firstNameError
  const lastNameError = registrationNameError(fields.lastName, 'Příjmení')
  if (lastNameError) errors.lastName = lastNameError
  const email = fields.email.trim()
  if (Array.from(email).length > 320 || !/^[^\s@]+@[^\s@]+$/u.test(email)) errors.email = 'Zadejte platnou e-mailovou adresu.'
  const passwordError = registrationPasswordError(fields.password)
  if (passwordError) errors.password = passwordError
  if (!fields.passwordConfirmation) errors.passwordConfirmation = 'Zadejte stejné heslo ještě jednou.'
  else if (fields.password !== fields.passwordConfirmation) errors.passwordConfirmation = 'Hesla se neshodují. Zadejte prosím stejné heslo do obou polí.'
  if (fields.accountType !== 'STUDENT' && fields.accountType !== 'TEACHER') errors.accountType = 'Vyberte, jestli jste člen týmu, nebo vyučující.'
  if (!fields.privacyAccepted) errors.privacyAccepted = 'Před vytvořením účtu potvrďte, že jste se seznámili se zásadami ochrany osobních údajů.'
  return errors
}

/** Založí účet a ověří odpověď 201; session nevzniká a uživatel se musí následně přihlásit. */
export async function registerAccount(fields: RegistrationFields, signal?: AbortSignal): Promise<RegisteredUser> {
  const payload = { firstName: fields.firstName, lastName: fields.lastName, email: fields.email, password: fields.password,
    accountType: fields.accountType, privacyAccepted: fields.privacyAccepted, privacyPolicyVersion }
  const response = await authPost('/api/auth/register', payload, signal)
  if (response.status === 201) {
    const value: unknown = await response.clone().json().catch(() => null)
    if (isRecord(value) && isAuthUser(value.user)) return value.user
  }
  const failure = await responseFailure(response, 'Vytvoření účtu se nepodařilo potvrdit. Zkuste se nejdřív přihlásit; pokud to nepůjde, registraci zopakujte později.')
  if (response.status === 400) failure.message = 'Ještě prosím zkontrolujte označené údaje.'
  if (response.status === 409) failure.message = 'S tímto e-mailem teď účet nelze vytvořit. Zkuste se přihlásit nebo použijte jiný e-mail.'
  if (response.status === 403) failure.message = 'Formulář se nepodařilo bezpečně ověřit. Obnovte stránku a zkuste to znovu.'
  if (response.status === 429) failure.message = 'Na chvíli jsme pozastavili další pokusy. Zkuste to prosím za chvíli.'
  throw failure
}
