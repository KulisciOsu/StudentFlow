import { afterEach, describe, expect, it, vi } from 'vitest'
import { csrfRequest, CsrfTokenError } from './csrfRequest'

const json = (value: unknown, status = 200) =>
  new Response(JSON.stringify(value), { status, headers: { 'Content-Type': 'application/json' } })
const csrf = (token = 'token-1') => json({ token, headerName: 'X-CSRF-TOKEN' })
afterEach(() => vi.unstubAllGlobals())

describe('csrfRequest', () => {
  it('sends the method, JSON body, session cookie and a fresh CSRF token', async () => {
    const fetch = vi.fn().mockResolvedValueOnce(csrf()).mockResolvedValueOnce(json({ ok: true }, 201))
    vi.stubGlobal('fetch', fetch)

    const response = await csrfRequest('POST', '/api/projects/7/members', { email: 'jan@example.test' })

    expect(response.status).toBe(201)
    expect(fetch.mock.calls[0][0]).toBe('/api/auth/csrf')
    const [path, options] = fetch.mock.calls[1]
    expect(path).toBe('/api/projects/7/members')
    expect(options.method).toBe('POST')
    expect(options.headers['X-CSRF-TOKEN']).toBe('token-1')
    expect(options.headers['Content-Type']).toBe('application/json')
    expect(options.credentials).toBe('same-origin')
    expect(options.cache).toBe('no-store')
    expect(JSON.parse(options.body)).toEqual({ email: 'jan@example.test' })
  })

  it('sends DELETE without a body or content type', async () => {
    const fetch = vi.fn().mockResolvedValueOnce(csrf()).mockResolvedValueOnce(new Response(null, { status: 204 }))
    vi.stubGlobal('fetch', fetch)

    const response = await csrfRequest('DELETE', '/api/projects/7/members/2')

    expect(response.status).toBe(204)
    const options = fetch.mock.calls[1][1]
    expect(options.method).toBe('DELETE')
    expect(options.body).toBeUndefined()
    expect(options.headers).toEqual({ 'X-CSRF-TOKEN': 'token-1' })
  })

  it('refreshes a rejected CSRF token once and then retries', async () => {
    const fetch = vi.fn().mockResolvedValueOnce(csrf()).mockResolvedValueOnce(json({ code: 'CSRF_INVALID' }, 403))
      .mockResolvedValueOnce(csrf('token-2')).mockResolvedValueOnce(new Response(null, { status: 204 }))
    vi.stubGlobal('fetch', fetch)

    const response = await csrfRequest('DELETE', '/api/projects/7/members/2')

    expect(response.status).toBe(204)
    expect(fetch).toHaveBeenCalledTimes(4)
    expect(fetch.mock.calls[3][1].headers['X-CSRF-TOKEN']).toBe('token-2')
  })

  it('returns the second CSRF rejection to the caller without a third attempt', async () => {
    const fetch = vi.fn().mockResolvedValueOnce(csrf()).mockResolvedValueOnce(json({ code: 'CSRF_INVALID' }, 403))
      .mockResolvedValueOnce(csrf('token-2')).mockResolvedValueOnce(json({ code: 'CSRF_INVALID' }, 403))
    vi.stubGlobal('fetch', fetch)

    const response = await csrfRequest('DELETE', '/api/projects/7/members/2')

    expect(response.status).toBe(403)
    expect(fetch).toHaveBeenCalledTimes(4)
  })

  it('returns other 403 errors unchanged and keeps the body readable', async () => {
    const fetch = vi.fn().mockResolvedValueOnce(csrf()).mockResolvedValueOnce(json({ code: 'PROJECT_LEADER_REQUIRED' }, 403))
    vi.stubGlobal('fetch', fetch)

    const response = await csrfRequest('DELETE', '/api/projects/7/members/2')

    expect(fetch).toHaveBeenCalledTimes(2)
    expect(await response.json()).toEqual({ code: 'PROJECT_LEADER_REQUIRED' })
  })

  it('does not retry an uncertain network failure after sending', async () => {
    const fetch = vi.fn().mockResolvedValueOnce(csrf()).mockRejectedValueOnce(new TypeError('network'))
    vi.stubGlobal('fetch', fetch)

    await expect(csrfRequest('DELETE', '/api/projects/7/members/2')).rejects.toThrow('network')
    expect(fetch).toHaveBeenCalledTimes(2)
  })

  it.each([
    ['a failed token request', () => json({}, 503)],
    ['an unexpected token header', () => json({ token: 'token-1', headerName: 'X-OTHER' })],
    ['an empty token', () => json({ token: '', headerName: 'X-CSRF-TOKEN' })],
  ])('throws CsrfTokenError and sends nothing after %s', async (_, tokenResponse) => {
    const fetch = vi.fn().mockResolvedValueOnce(tokenResponse())
    vi.stubGlobal('fetch', fetch)

    await expect(csrfRequest('POST', '/api/projects/7/members', {})).rejects.toBeInstanceOf(CsrfTokenError)
    expect(fetch).toHaveBeenCalledTimes(1)
  })
})
