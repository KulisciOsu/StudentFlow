import { afterEach, describe, expect, it, vi } from 'vitest'
import { fetchIdentity, loginAccount, logoutAccount, validateLogin } from './session'
import { AuthFailure } from './client'
import { homeFor } from './types'

const fields = { email: 'jan@example.test', password: 'moje-dlouhe-heslo-2026', rememberMe: false }
const identity = { user: { id: '42', displayName: 'Jan Novák', email: fields.email, accountType: 'STUDENT' as const }, session: { expiresAt: '2026-11-03T12:00:00Z', persistent: false } }
const json = (value: unknown, status = 200, headers = {}) => new Response(JSON.stringify(value), { status, headers: { 'Content-Type': 'application/json', ...headers } })
const csrf = () => json({ token: 'token', headerName: 'X-CSRF-TOKEN' })
afterEach(() => vi.unstubAllGlobals())

describe('session API client', () => {
  it('restores only a valid identity and sends credentials without cache', async () => {
    const fetch = vi.fn().mockResolvedValue(json(identity)); vi.stubGlobal('fetch', fetch)
    expect(await fetchIdentity()).toEqual(identity)
    expect(fetch).toHaveBeenCalledWith('/api/auth/me', expect.objectContaining({ credentials: 'same-origin', cache: 'no-store' }))
  })
  it('distinguishes anonymous 401 from failed verification', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(json({}, 401)))
    expect(await fetchIdentity()).toBeNull()
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(json({}, 503)))
    await expect(fetchIdentity()).rejects.toBeInstanceOf(AuthFailure)
    vi.stubGlobal('fetch', vi.fn().mockRejectedValue(new TypeError('network')))
    await expect(fetchIdentity()).rejects.toThrow('network')
  })
  it.each([
    { ...identity, user: { ...identity.user, accountType: ['STUDENT', 'TEACHER'] } },
    { ...identity, user: { ...identity.user, id: 42 } },
    { ...identity, session: { ...identity.session, expiresAt: 'invalid' } },
    { ...identity, session: { ...identity.session, persistent: 'true' } },
    { ...identity, token: 'secret' },
  ])('rejects malformed identity %#', async value => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(json(value)))
    await expect(fetchIdentity()).rejects.toBeInstanceOf(AuthFailure)
  })
  it('sends only login fields and restores the server identity', async () => {
    const fetch = vi.fn().mockResolvedValueOnce(csrf()).mockResolvedValueOnce(json(identity)); vi.stubGlobal('fetch', fetch)
    expect(await loginAccount(fields)).toEqual(identity)
    expect(JSON.parse(fetch.mock.calls[1][1].body)).toEqual(fields)
    expect(fetch.mock.calls[1][1].headers['X-CSRF-TOKEN']).toBe('token')
  })
  it('keeps invalid credentials generic and does not automatically retry', async () => {
    const fetch = vi.fn().mockResolvedValueOnce(csrf()).mockResolvedValueOnce(json({ code: 'INVALID_CREDENTIALS' }, 401)); vi.stubGlobal('fetch', fetch)
    await expect(loginAccount(fields)).rejects.toMatchObject({ message: 'E-mail nebo heslo nejsou správné.', status: 401 })
    expect(fetch).toHaveBeenCalledTimes(2)
  })
  it('does not repeat a login whose outcome is unknown', async () => {
    const fetch = vi.fn().mockResolvedValueOnce(csrf()).mockResolvedValueOnce(json({}, 503)); vi.stubGlobal('fetch', fetch)
    await expect(loginAccount(fields)).rejects.toMatchObject({ status: 503 }); expect(fetch).toHaveBeenCalledTimes(2)
  })
  it('confirms logout only for 204 and sends no request body', async () => {
    const fetch = vi.fn().mockResolvedValueOnce(csrf()).mockResolvedValueOnce(new Response(null, { status: 204 })); vi.stubGlobal('fetch', fetch)
    await expect(logoutAccount()).resolves.toBeUndefined()
    expect(fetch.mock.calls[1][1].body).toBeUndefined()
    vi.stubGlobal('fetch', vi.fn().mockResolvedValueOnce(csrf()).mockResolvedValueOnce(json({}, 503)))
    await expect(logoutAccount()).rejects.toBeInstanceOf(AuthFailure)
  })
  it('honors a login rate limit without scheduling another POST', async () => {
    const fetch = vi.fn().mockResolvedValueOnce(csrf()).mockResolvedValueOnce(json({}, 429, { 'Retry-After': '45' })); vi.stubGlobal('fetch', fetch)
    await expect(loginAccount(fields)).rejects.toMatchObject({ retryAfter: 45, status: 429 }); expect(fetch).toHaveBeenCalledTimes(2)
  })
})

describe('login form and navigation rules', () => {
  it('requires email and a valid password, counting Unicode code points', () => {
    expect(validateLogin({ ...fields, email: 'bad', password: '🦉'.repeat(14) })).toHaveProperty('email')
    expect(validateLogin({ ...fields, password: '🦉'.repeat(15) })).toEqual({})
    expect(validateLogin({ ...fields, password: '🦉'.repeat(128) })).toEqual({})
    expect(validateLogin({ ...fields, password: '🦉'.repeat(129) })).toHaveProperty('password')
  })
  it('chooses the single account type home', () => {
    expect(homeFor(identity.user)).toBe('/projects')
    expect(homeFor({ ...identity.user, accountType: 'TEACHER' })).toBe('/teaching')
  })
})
