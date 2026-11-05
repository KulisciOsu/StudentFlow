import { isRecord } from './types'

/** Chyba použitelná ve formuláři; převádí odpověď API na zprávu, chyby polí a čekání na další pokus. */
export class AuthFailure extends Error {
  readonly errors: Partial<Record<'displayName' | 'email' | 'password' | 'accountType', string>>
  /** Počet sekund do dalšího pokusu po omezení frekvence; nula znamená bez čekání. */
  readonly retryAfter: number
  /** HTTP status; nula znamená, že stav odpovědi operace nebyl předán. */
  readonly status: number
  constructor(message: string, errors: AuthFailure['errors'] = {}, retryAfter = 0, status = 0) {
    super(message)
    this.errors = errors
    this.retryAfter = retryAfter
    this.status = status
  }
}

/**
 * Odešle POST se session cookie a čerstvým CSRF tokenem pro aktuální identitu.
 * Pouze po CSRF_INVALID obnoví token a požadavek jednou zopakuje; ostatní HTTP chyby vrátí volajícímu.
 */
export async function authPost(path: string, body: unknown, signal?: AbortSignal): Promise<Response> {
  async function csrf() {
    const response = await fetch('/api/auth/csrf', { signal, credentials: 'same-origin', cache: 'no-store' })
    if (!response.ok) throw new AuthFailure('Ověření požadavku se nepodařilo. Zkuste to znovu.')
    const value: unknown = await response.json()
    if (!isRecord(value) || typeof value.token !== 'string' || !value.token || value.headerName !== 'X-CSRF-TOKEN')
      throw new AuthFailure('Ověření požadavku se nepodařilo. Zkuste to znovu.')
    return value.token
  }
  let token = await csrf()
  for (let attempt = 0; attempt < 2; attempt++) {
    const response = await fetch(path, {
      method: 'POST', signal, credentials: 'same-origin', cache: 'no-store',
      headers: { ...(body === undefined ? {} : { 'Content-Type': 'application/json' }), 'X-CSRF-TOKEN': token },
      body: body === undefined ? undefined : JSON.stringify(body),
    })
    if (response.status === 403 && attempt === 0) {
      const problem: unknown = await response.clone().json().catch(() => null)
      if (isRecord(problem) && problem.code === 'CSRF_INVALID') { token = await csrf(); continue }
    }
    return response
  }
  throw new AuthFailure('Ověření požadavku se nepodařilo. Zkuste to znovu.')
}

/** Převede známé HTTP chyby a Retry-After na zprávu pro UI; neznámé chyby použijí předaný fallback. */
export async function responseFailure(response: Response, fallback: string): Promise<AuthFailure> {
  const value: unknown = await response.json().catch(() => null)
  const errors: AuthFailure['errors'] = {}
  if (isRecord(value) && isRecord(value.errors)) {
    for (const field of ['displayName', 'email', 'password', 'accountType'] as const) {
      const messages = value.errors[field]
      if (Array.isArray(messages) && typeof messages[0] === 'string') errors[field] = messages[0]
    }
  }
  if (response.status === 429) {
    const seconds = Number(response.headers.get('Retry-After'))
    return new AuthFailure('Příliš mnoho pokusů. Zkuste to znovu později.', {},
      Number.isFinite(seconds) && seconds > 0 ? Math.ceil(seconds) : 60, 429)
  }
  const message = response.status === 409 ? 'Účet s tímto e-mailem už existuje.'
    : response.status === 400 ? 'Opravte označené údaje.'
      : response.status === 401 ? 'E-mail nebo heslo nejsou správné.' : fallback
  return new AuthFailure(message, errors, 0, response.status)
}
