import { afterEach, describe, expect, it, vi } from 'vitest'
import { registerAccount, RegistrationFailure, validateRegistration } from './registration'
import type { RegistrationFields } from './registration'

const fields: RegistrationFields = { firstName: 'Jan', lastName: 'Novák', email: 'jan@example.test', password: 'TestHeslo1!', passwordConfirmation: 'TestHeslo1!', accountType: 'STUDENT', privacyAccepted: true }
const user = { id: '42', displayName: 'Jan Novák', email: 'jan@example.test', accountType: 'STUDENT' }
const json = (value: unknown, status = 200, headers = {}) => new Response(JSON.stringify(value), { status, headers: { 'Content-Type': 'application/json', ...headers } })
const csrf = (token = 'token-1') => json({ token, headerName: 'X-CSRF-TOKEN' })
afterEach(() => vi.unstubAllGlobals())

describe('registration validation', () => {
  it.each(['STUDENT', 'TEACHER'] as const)('accepts exactly one type %s', accountType => {
    expect(validateRegistration({ ...fields, accountType })).toEqual({})
  })
  it.each([7, 16])('rejects password length %i', length => {
    const password = 'A1!' + 'x'.repeat(length - 3)
    expect(validateRegistration({ ...fields, password, passwordConfirmation: password }).password).toBeTruthy()
  })
  it.each([8, 15])('accepts Unicode password length %i', length => {
    const password = 'A1!' + '🦉'.repeat(length - 3)
    expect(validateRegistration({ ...fields, password, passwordConfirmation: password })).toEqual({})
  })
  it('requires a matching confirmation and a chosen type', () => {
    const errors = validateRegistration({ ...fields, accountType: '', passwordConfirmation: 'different' })
    expect(errors.accountType).toBeTruthy()
    expect(errors.passwordConfirmation).toBeTruthy()
  })
  it('rejects blank name and malformed email', () => {
    const errors = validateRegistration({ ...fields, firstName: ' ', email: 'bad-address' })
    expect(errors.firstName).toBeTruthy()
    expect(errors.email).toBeTruthy()
  })
  it.each(['firstName', 'lastName'] as const)('requires %s separately and rejects all whitespace', field => {
    for (const value of ['', ' Jan', 'Jan ', 'Jan Pavel', 'Jan\tPavel', 'Jan\nPavel', 'Jan\u00a0Pavel', 'Jan\u0085Pavel', 'Jan\u2003Pavel', 'Jan\u3000Pavel', 'Jan\ufeffPavel']) {
      expect(validateRegistration({ ...fields, [field]: value })[field]).toBeTruthy()
    }
    for (const length of [1, 50]) {
      expect(validateRegistration({ ...fields, [field]: '𐐀'.repeat(length) })).toEqual({})
    }
    expect(validateRegistration({ ...fields, [field]: '𐐀'.repeat(51) })[field]).toBeTruthy()
    expect(validateRegistration({ ...fields, [field]: 'NovákSvoboda' })).toEqual({})
  })
  it.each(['firstName', 'lastName'] as const)('allows only letters and diacritics in %s', field => {
    for (const value of ['Jan', 'Žofie', 'Nova\u0301k', 'किरण', '李']) {
      expect(validateRegistration({ ...fields, [field]: value })).toEqual({})
    }
    for (const value of ['Jan1', '1Jan', 'Jan١', 'Jan１', 'JanⅣ', 'Novák-Svoboda', 'O’Neill', "O'Neill", 'Novák.', 'Jan@', 'Jan_', 'Jan🦉', 'Jan\u200b', '\u0301Jan', 'A\u20dd']) {
      expect(validateRegistration({ ...fields, [field]: value })[field]).toBe(`${field === 'firstName' ? 'Jméno' : 'Příjmení'} může obsahovat pouze písmena a diakritiku.`)
    }
  })
  it('explains the name length only for an overlong value', () => {
    expect(validateRegistration({ ...fields, firstName: '' }).firstName).toBe('Vyplňte prosím pole „Jméno“.')
    expect(validateRegistration({ ...fields, firstName: 'A'.repeat(51) }).firstName).toBe('Jméno může mít nejvýše 50 znaků.')
  })
  it.each(['testheslo1!', 'TestHeslo!!', 'TestHeslo12', ' TestHeslo1!', 'TestHeslo1! ', 'Test\tHeslo1!', 'Test\nHeslo1!', 'Test\u00a0Heslo1!', 'Test\u0085Heslo1!', 'Test\ufeffHeslo1!'])('rejects an incomplete or whitespace password: %j', password => {
    expect(validateRegistration({ ...fields, password, passwordConfirmation: password }).password).toBeTruthy()
  })
  it('does not add a lowercase requirement and requires explicit privacy confirmation', () => {
    expect(validateRegistration({ ...fields, password: 'ABCDEFG1!', passwordConfirmation: 'ABCDEFG1!' })).toEqual({})
    expect(validateRegistration({ ...fields, privacyAccepted: false })).toHaveProperty('privacyAccepted')
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
    expect(JSON.parse(options.body)).toEqual({ firstName: fields.firstName, lastName: fields.lastName, email: fields.email, password: fields.password, accountType: 'STUDENT', privacyAccepted: true, privacyPolicyVersion: '2026-11-20.2' })
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
  it('keeps conflicts neutral and respects Retry-After', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValueOnce(csrf()).mockResolvedValueOnce(json({ code: 'REGISTRATION_CONFLICT', errors: { email: ['Účet již existuje.'] } }, 409)))
    await expect(registerAccount(fields)).rejects.toMatchObject({ message: expect.stringContaining('S tímto e-mailem teď účet nelze vytvořit.'), errors: {} })
    vi.stubGlobal('fetch', vi.fn().mockResolvedValueOnce(csrf()).mockResolvedValueOnce(json({ code: 'RATE_LIMITED' }, 429, { 'Retry-After': '30' })))
    await expect(registerAccount(fields)).rejects.toMatchObject({ retryAfter: 30 })
  })
  it('passes safe privacy validation and hides unexpected server details', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValueOnce(csrf()).mockResolvedValueOnce(json({ code: 'VALIDATION_FAILED', errors: { privacyAccepted: ['Potvrďte seznámení se zásadami.'] } }, 400)))
    await expect(registerAccount(fields)).rejects.toMatchObject({ errors: { privacyAccepted: 'Potvrďte seznámení se zásadami.' } })
    vi.stubGlobal('fetch', vi.fn().mockResolvedValueOnce(csrf()).mockResolvedValueOnce(json({ detail: 'sensitive-database', errors: { password: ['sensitive-database'] } }, 503)))
    await expect(registerAccount(fields)).rejects.toMatchObject({ message: expect.not.stringContaining('sensitive-database'), errors: {} })
  })
})
