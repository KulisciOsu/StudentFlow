import { afterEach, describe, expect, it, vi } from 'vitest'
import { addProjectMember, getProjectMembers, ProjectMembersFailure, removeProjectMember } from './projectMembersApi'

const petra = { userId: '1', displayName: 'Petra Svobodová', email: 'petra@example.test', isLeader: true, addedAt: '2026-11-07T12:00:00Z' }
const jan = { userId: '2', displayName: 'Jan Novák', email: 'jan@example.test', isLeader: false, addedAt: '2026-11-07T12:30:00Z' }
const json = (value: unknown, status = 200) =>
  new Response(JSON.stringify(value), { status, headers: { 'Content-Type': 'application/json' } })
const csrf = () => json({ token: 'token-1', headerName: 'X-CSRF-TOKEN' })
afterEach(() => vi.unstubAllGlobals())

describe('getProjectMembers', () => {
  it('loads the team name and members with the session cookie and without cache', async () => {
    const fetch = vi.fn().mockResolvedValueOnce(json({ teamName: 'Tým Sova', members: [petra, jan] }))
    vi.stubGlobal('fetch', fetch)

    expect(await getProjectMembers(7)).toEqual({ teamName: 'Tým Sova', members: [petra, jan] })
    const [path, options] = fetch.mock.calls[0]
    expect(path).toBe('/api/projects/7/members')
    expect(options.credentials).toBe('same-origin')
    expect(options.cache).toBe('no-store')
  })

  it.each([
    ['a numeric user id', { teamName: 'Tým Sova', members: [{ ...jan, userId: 2 }] }],
    ['an empty user id', { teamName: 'Tým Sova', members: [{ ...jan, userId: '' }] }],
    ['a missing leader flag', { teamName: 'Tým Sova', members: [{ ...jan, isLeader: undefined }] }],
    ['an unreadable date', { teamName: 'Tým Sova', members: [{ ...jan, addedAt: 'včera' }] }],
    ['a missing team name', { members: [petra] }],
    ['members that are not a list', { teamName: 'Tým Sova', members: petra }],
  ])('rejects a response with %s', async (_, body) => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValueOnce(json(body)))

    await expect(getProjectMembers(7)).rejects.toMatchObject({
      name: 'ProjectMembersFailure', message: 'Odpověď serveru se nepodařilo zpracovat. Obnovte stránku.' })
  })

  it('reports a missing or foreign project with one shared message', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValueOnce(json({ code: 'PROJECT_NOT_FOUND' }, 404)))

    await expect(getProjectMembers(7)).rejects.toMatchObject({
      status: 404, code: 'PROJECT_NOT_FOUND', message: 'Projekt neexistuje nebo k němu nemáte přístup.' })
  })

  it('reports an expired session even without a problem body', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValueOnce(new Response(null, { status: 401 })))

    await expect(getProjectMembers(7)).rejects.toMatchObject({
      status: 401, code: null, message: 'Vaše přihlášení vypršelo. Přihlaste se prosím znovu.' })
  })

  it('uses the general load message for unknown errors', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValueOnce(json({ code: 'SERVICE_UNAVAILABLE' }, 503)))

    await expect(getProjectMembers(7)).rejects.toMatchObject({ status: 503, message: 'Členy projektu se nepodařilo načíst.' })
  })
})

describe('addProjectMember', () => {
  it('posts the trimmed e-mail with a CSRF token and returns the new member', async () => {
    const fetch = vi.fn().mockResolvedValueOnce(csrf()).mockResolvedValueOnce(json({ member: jan }, 201))
    vi.stubGlobal('fetch', fetch)

    expect(await addProjectMember(7, '  jan@example.test ')).toEqual(jan)
    const [path, options] = fetch.mock.calls[1]
    expect(path).toBe('/api/projects/7/members')
    expect(options.method).toBe('POST')
    expect(options.headers['X-CSRF-TOKEN']).toBe('token-1')
    expect(JSON.parse(options.body)).toEqual({ email: 'jan@example.test' })
  })

  it.each([
    [400, 'VALIDATION_FAILED', 'Zadejte platnou e-mailovou adresu, nejvýše 320 znaků.'],
    [403, 'PROJECT_LEADER_REQUIRED', 'Členy projektu může spravovat jen vedoucí týmu.'],
    [404, 'PROJECT_NOT_FOUND', 'Projekt neexistuje nebo k němu nemáte přístup.'],
    [409, 'MEMBER_ALREADY_EXISTS', 'Tento uživatel už je členem týmu.'],
    [422, 'ACCOUNT_NOT_FOUND', 'Účet s tímto e-mailem v StudentFlow neexistuje.'],
    [422, 'ACCOUNT_NOT_STUDENT', 'Do týmu lze přidat jen účet studenta.'],
  ])('maps %i %s to its message', async (status, code, message) => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValueOnce(csrf()).mockResolvedValueOnce(json({ code }, status)))

    const failure = await addProjectMember(7, 'jan@example.test').catch((error: unknown) => error)

    expect(failure).toBeInstanceOf(ProjectMembersFailure)
    expect(failure).toMatchObject({ status, code, message })
  })

  it.each(['INTERNAL_ERROR', 'constructor'])('uses the general add message for unknown code %s', async code => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValueOnce(csrf()).mockResolvedValueOnce(json({ code }, 500)))

    await expect(addProjectMember(7, 'jan@example.test')).rejects.toMatchObject({ code, message: 'Člena se nepodařilo přidat.' })
  })

  it('rejects a success response without a valid member', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValueOnce(csrf()).mockResolvedValueOnce(json({ user: jan }, 201)))

    await expect(addProjectMember(7, 'jan@example.test')).rejects.toMatchObject({
      message: 'Odpověď serveru se nepodařilo zpracovat. Obnovte stránku.' })
  })
})

describe('removeProjectMember', () => {
  it('deletes the member by user id with a CSRF token and no body', async () => {
    const fetch = vi.fn().mockResolvedValueOnce(csrf()).mockResolvedValueOnce(new Response(null, { status: 204 }))
    vi.stubGlobal('fetch', fetch)

    await expect(removeProjectMember(7, '2')).resolves.toBeUndefined()
    const [path, options] = fetch.mock.calls[1]
    expect(path).toBe('/api/projects/7/members/2')
    expect(options.method).toBe('DELETE')
    expect(options.headers['X-CSRF-TOKEN']).toBe('token-1')
    expect(options.body).toBeUndefined()
  })

  it.each([
    [403, 'PROJECT_LEADER_REQUIRED', 'Členy projektu může spravovat jen vedoucí týmu.'],
    [404, 'MEMBER_NOT_FOUND', 'Tento uživatel není členem projektu.'],
    [409, 'LEADER_CANNOT_BE_REMOVED', 'Vedoucího týmu nelze z projektu odebrat.'],
  ])('maps %i %s to its message', async (status, code, message) => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValueOnce(csrf()).mockResolvedValueOnce(json({ code }, status)))

    await expect(removeProjectMember(7, '2')).rejects.toMatchObject({ status, code, message })
  })

  it('uses the general remove message for unknown errors', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValueOnce(csrf()).mockResolvedValueOnce(new Response(null, { status: 500 })))

    await expect(removeProjectMember(7, '2')).rejects.toMatchObject({ status: 500, code: null, message: 'Člena se nepodařilo odebrat.' })
  })
})
