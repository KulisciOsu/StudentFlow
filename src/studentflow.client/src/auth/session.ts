import { authPost, responseFailure, AuthFailure } from './client'
import { isIdentity } from './types'
import type { Identity } from './types'

export type LoginFields = { email: string; password: string; rememberMe: boolean }
export type LoginErrors = Partial<Record<'email' | 'password', string>>
export function validateLogin(fields: LoginFields): LoginErrors {
  const errors: LoginErrors = {}
  if (Array.from(fields.email.trim()).length > 320 || !/^[^\s@]+@[^\s@]+$/u.test(fields.email.trim())) errors.email = 'Zadejte platnou e-mailovou adresu.'
  if (Array.from(fields.password).length < 15 || Array.from(fields.password).length > 128) errors.password = 'Heslo musí mít 15 až 128 znaků.'
  return errors
}
async function readIdentity(response: Response): Promise<Identity> {
  const value: unknown = await response.json().catch(() => null)
  if (!isIdentity(value)) throw new AuthFailure('Přihlášení se nepodařilo ověřit. Zkuste to znovu.')
  return value
}
export async function fetchIdentity(signal?: AbortSignal): Promise<Identity | null> {
  const response = await fetch('/api/auth/me', { signal, credentials: 'same-origin', cache: 'no-store' })
  if (response.status === 401) return null
  if (response.status !== 200) throw await responseFailure(response, 'Přihlášení se nepodařilo ověřit. Zkuste to znovu.')
  return readIdentity(response)
}
export async function loginAccount(fields: LoginFields, signal?: AbortSignal): Promise<Identity> {
  const response = await authPost('/api/auth/login', fields, signal)
  if (response.status !== 200) throw await responseFailure(response, 'Přihlášení se nepodařilo potvrdit. Můžete ověřit jeho stav.')
  return readIdentity(response)
}
export async function logoutAccount(signal?: AbortSignal): Promise<void> {
  const response = await authPost('/api/auth/logout', undefined, signal)
  if (response.status !== 204) throw await responseFailure(response, 'Odhlášení se nepodařilo potvrdit. Zkuste to znovu.')
}
