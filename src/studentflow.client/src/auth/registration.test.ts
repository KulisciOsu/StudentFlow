import { afterEach, describe, expect, it, vi } from 'vitest'
import { registerAccount, RegistrationFailure, validateRegistration } from './registration'
import type { RegistrationFields } from './registration'

const fields: RegistrationFields = { displayName: 'Jan Novák', email: 'jan@example.test', password: 'moje-dlouhe-heslo-2026', passwordConfirmation: 'moje-dlouhe-heslo-2026', accountType: 'STUDENT' }
const user = { id: '42', displayName: 'Jan Novák', email: 'jan@example.test', accountType: 'STUDENT' }
const json = (value: unknown, status = 200, headers = {}) => new Response(JSON.stringify(value), { status, headers: { 'Content-Type': 'application/json', ...headers } })
const csrf = (token = 'token-1') => json({ token, headerName: 'X-CSRF-TOKEN' })
afterEach(() => vi.unstubAllGlobals())

describe('registration validation', () => {
  it.each(['STUDENT', 'TEACHER'] as const)('accepts exactly one type %s', accountType => {
    expect(validateRegistration({ ...fields, accountType })).toEqual({})
  })
  it.each([14, 129])('rejects password length %i', length => {
    const password = 'x'.repeat(length)
    expect(validateRegistration({ ...fields, password, passwordConfirmation: password }).password).toBeTruthy()
  })
  it.each([15, 128])('accepts Unicode password length %i without trimming', length => {
    const password = '🦉'.repeat(length)
    expect(validateRegistration({ ...fields, password, passwordConfirmation: password })).toEqual({})
  })
  it('requires a matching confirmation and a chosen type', () => {
    const errors = validateRegistration({ ...fields, accountType: '', passwordConfirmation: 'different' })
    expect(errors.accountType).toBeTruthy()
    expect(errors.passwordConfirmation).toBeTruthy()
  })
  it('rejects blank name and malformed email', () => {
    const errors = validateRegistration({ ...fields, displayName: ' ', email: 'bad-address' })
    expect(errors.displayName).toBeTruthy()
    expect(errors.email).toBeTruthy()
  })
})

describe('registration API client', () => {
  it('sends CSRF and exactly the public request fields, with no password confirmation', async () => {
    const fetch = vi.fn().mockResolvedValueOnce(csrf()).mockResolvedValueOnce(json({ user }, 201))
    vi.stubGlobal('fetch', fetch)
    expect(await registerAccount(fields)).toEqual(user)
    const options = fetch.mock.calls[1][1]
    expect(options.headers['X-CSRF-TOKEN']).toBe('token-1')
    expect(options.credentials).toBe('same-origin')
    expect(JSON.parse(options.body)).toEqual({ displayName: fields.displayName, email: fields.email, password: fields.password, accountType: 'STUDENT' })
  })
  it('refreshes a rejected CSRF token once and then retries', async () => {
    const fetch = vi.fn().mockResolvedValueOnce(csrf()).mockResolvedValueOnce(json({ code: 'CSRF_INVALID' }, 403))
      .mockResolvedValueOnce(csrf('token-2')).mockResolvedValueOnce(json({ user }, 201))
    vi.stubGlobal('fetch', fetch)
    expect(await registerAccount(fields)).toEqual(user)
    expect(fetch).toHaveBeenCalledTimes(4)
    expect(fetch.mock.calls[3][1].headers['X-CSRF-TOKEN']).toBe('token-2')
  })
  it('stops after the second CSRF rejection', async () => {
    const fetch = vi.fn().mockResolvedValueOnce(csrf()).mockResolvedValueOnce(json({ code: 'CSRF_INVALID' }, 403))
      .mockResolvedValueOnce(csrf('token-2')).mockResolvedValueOnce(json({ code: 'CSRF_INVALID' }, 403))
    vi.stubGlobal('fetch', fetch)
    await expect(registerAccount(fields)).rejects.toBeInstanceOf(RegistrationFailure)
    expect(fetch).toHaveBeenCalledTimes(4)
  })
  it('does not retry an uncertain network failure after posting', async () => {
    const fetch = vi.fn().mockResolvedValueOnce(csrf()).mockRejectedValueOnce(new TypeError('network'))
    vi.stubGlobal('fetch', fetch)
    await expect(registerAccount(fields)).rejects.toThrow('network')
    expect(fetch).toHaveBeenCalledTimes(2)
  })
  it('does not claim success for a malformed 201 response', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValueOnce(csrf()).mockResolvedValueOnce(json({ user: { ...user, id: 42 } }, 201)))
    await expect(registerAccount(fields)).rejects.toBeInstanceOf(RegistrationFailure)
  })
  it('passes field errors for a duplicate and respects Retry-After', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValueOnce(csrf()).mockResolvedValueOnce(json({ code: 'EMAIL_ALREADY_REGISTERED', errors: { email: ['Účet již existuje.'] } }, 409)))
    await expect(registerAccount(fields)).rejects.toMatchObject({ errors: { email: 'Účet již existuje.' } })
    vi.stubGlobal('fetch', vi.fn().mockResolvedValueOnce(csrf()).mockResolvedValueOnce(json({ code: 'RATE_LIMITED' }, 429, { 'Retry-After': '30' })))
    await expect(registerAccount(fields)).rejects.toMatchObject({ retryAfter: 30 })
  })
})
