import { isRecord } from '../../auth/types'

/// <summary>
/// HTTP metody, které mění data a které API chrání CSRF tokenem.
/// </summary>
export type WriteMethod = 'POST' | 'PUT' | 'PATCH' | 'DELETE'

/// <summary>
/// CSRF token se nepodařilo získat, proto se samotný požadavek neodeslal.
/// </summary>
export class CsrfTokenError extends Error {
  constructor() {
    super('Ověření požadavku se nepodařilo. Zkuste to znovu.')
    this.name = 'CsrfTokenError'
  }
}

/// <summary>
/// Odešle požadavek měnící data se session cookie a čerstvým CSRF tokenem.
/// Pouze po odpovědi 403 s kódem CSRF_INVALID obnoví token a požadavek jednou zopakuje;
/// ostatní odpovědi včetně chyb vrátí volajícímu beze změny. Chyba sítě se neopakuje,
/// protože server mohl požadavek už provést. Pravidla odpovídají authPost z autentizace.
/// </summary>
export async function csrfRequest(method: WriteMethod, path: string, body?: unknown,
  signal?: AbortSignal): Promise<Response> {
  const send = (token: string) => fetch(path, {
    method, signal, credentials: 'same-origin', cache: 'no-store',
    headers: { ...(body === undefined ? {} : { 'Content-Type': 'application/json' }), 'X-CSRF-TOKEN': token },
    body: body === undefined ? undefined : JSON.stringify(body),
  })

  const response = await send(await fetchCsrfToken(signal))
  if (response.status === 403 && await isCsrfRejection(response)) return send(await fetchCsrfToken(signal))
  return response
}

/// <summary>
/// Načte nový CSRF token pro aktuální session; neplatnou odpověď ohlásí jako CsrfTokenError.
/// </summary>
async function fetchCsrfToken(signal?: AbortSignal): Promise<string> {
  const response = await fetch('/api/auth/csrf', { signal, credentials: 'same-origin', cache: 'no-store' })
  if (!response.ok) throw new CsrfTokenError()
  const value: unknown = await response.json().catch(() => null)
  if (!isRecord(value) || typeof value.token !== 'string' || !value.token || value.headerName !== 'X-CSRF-TOKEN')
    throw new CsrfTokenError()
  return value.token
}

/// <summary>
/// Zjistí, zda server odmítl právě CSRF token; tělo čte z kopie, aby ho volající mohl přečíst znovu.
/// </summary>
async function isCsrfRejection(response: Response): Promise<boolean> {
  const problem: unknown = await response.clone().json().catch(() => null)
  return isRecord(problem) && problem.code === 'CSRF_INVALID'
}
