import { afterEach, describe, expect, it, vi } from 'vitest'
import { AuthFailure, authPost } from './client'

const json = (value: unknown, status = 200) =>
  new Response(JSON.stringify(value), { status, headers: { 'Content-Type': 'application/json' } })
afterEach(() => vi.unstubAllGlobals())

describe('authPost', () => {
  it.each([
    ['a failed token request', () => json({}, 503)],
    ['an invalid token response', () => json({ token: '', headerName: 'X-CSRF-TOKEN' })],
  ])('reports %s as AuthFailure and sends nothing', async (_, tokenResponse) => {
    const fetch = vi.fn().mockResolvedValueOnce(tokenResponse())
    vi.stubGlobal('fetch', fetch)

    const failure = await authPost('/api/auth/login', {}).catch((error: unknown) => error)

    expect(failure).toBeInstanceOf(AuthFailure)
    expect(failure).toMatchObject({ message: 'Ověření požadavku se nepodařilo. Zkuste to znovu.' })
    expect(fetch).toHaveBeenCalledTimes(1)
  })
})
