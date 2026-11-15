// @vitest-environment jsdom
import { act, cleanup, fireEvent, render, screen, within } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import type { ProjectMember } from '../../../models/projects/ProjectMemberTypes'
import { addProjectMember, ProjectMembersFailure, removeProjectMember } from '../../../services/projects/projectMembersApi'
import { ProjectMembers } from './ProjectMembers'

const { refresh } = vi.hoisted(() => ({ refresh: vi.fn() }))

vi.mock('../../../auth/AuthContext', () => ({ useAuth: () => ({ refresh }) }))
vi.mock('../../../services/projects/projectMembersApi', async importOriginal => ({
  ...await importOriginal<typeof import('../../../services/projects/projectMembersApi')>(),
  addProjectMember: vi.fn(),
  removeProjectMember: vi.fn(),
}))

const members: ProjectMember[] = [
  { userId: '1', displayName: 'Petra Svobodová', email: 'petra@example.test', isLeader: true, joinedAt: '2026-11-07T12:00:00Z' },
  { userId: '2', displayName: 'Jan Novák', email: 'jan@example.test', isLeader: false, joinedAt: '2026-11-07T12:30:00Z' },
  { userId: '3', displayName: 'Lucie Černá', email: 'lucie@example.test', isLeader: false, joinedAt: '2026-11-15T10:00:00Z' },
]
const onChanged = vi.fn()

/// <summary>
/// Vykreslí sekci členů projektu 7 pohledem vedoucí Petry.
/// </summary>
function renderMembers() {
  render(<ProjectMembers projectId={7} onChanged={onChanged} teamName="Tým Sova" members={members} currentUserId="1" />)
}

/// <summary>
/// Vrátí řádek seznamu s daným jménem.
/// </summary>
function row(name: string): HTMLElement {
  return screen.getAllByRole('listitem').find(item => item.textContent?.includes(name))!
}

/// <summary>
/// Otevře potvrzení u člena a potvrdí odebrání.
/// </summary>
async function removeMember(name: string) {
  fireEvent.click(within(row(name)).getByRole('button', { name: 'Odebrat' }))
  await act(async () => { fireEvent.click(within(row(name)).getByRole('button', { name: 'Odebrat' })) })
}

/// <summary>
/// Vyplní e-mail ve formuláři „Přidat člena“ a odešle ho.
/// </summary>
async function addMember(email: string) {
  const input = screen.getByRole<HTMLInputElement>('textbox', { name: 'E-mail registrovaného studenta' })
  fireEvent.change(input, { target: { value: email } })
  await act(async () => { fireEvent.submit(input.form!) })
}

beforeEach(() => {
  refresh.mockReset().mockResolvedValue(undefined)
  onChanged.mockReset()
  vi.mocked(addProjectMember).mockReset()
  vi.mocked(removeProjectMember).mockReset()
})
afterEach(cleanup)

describe('ProjectMembers removal', () => {
  it('offers "Odebrat" on every row except the leader\'s', () => {
    renderMembers()

    expect(within(row('Petra Svobodová')).queryByRole('button')).toBeNull()
    expect(within(row('Jan Novák')).getByRole('button', { name: 'Odebrat' })).toBeTruthy()
    expect(within(row('Lucie Černá')).getByRole('button', { name: 'Odebrat' })).toBeTruthy()
  })

  it('asks "Opravdu odebrat?" in the row, focuses "Zrušit" and calls nothing yet', () => {
    renderMembers()

    fireEvent.click(within(row('Jan Novák')).getByRole('button', { name: 'Odebrat' }))

    const confirmation = screen.getByRole('group', { name: 'Opravdu odebrat? Jan Novák' })
    expect(within(confirmation).getByRole('button', { name: 'Odebrat' })).toBeTruthy()
    expect(document.activeElement).toBe(within(confirmation).getByRole('button', { name: 'Zrušit' }))
    expect(removeProjectMember).not.toHaveBeenCalled()
  })

  it('keeps only one confirmation open and closes it with "Zrušit"', () => {
    renderMembers()

    fireEvent.click(within(row('Jan Novák')).getByRole('button', { name: 'Odebrat' }))
    fireEvent.click(within(row('Lucie Černá')).getByRole('button', { name: 'Odebrat' }))
    expect(screen.getAllByRole('group')).toHaveLength(1)
    expect(screen.getByRole('group', { name: 'Opravdu odebrat? Lucie Černá' })).toBeTruthy()

    fireEvent.click(screen.getByRole('button', { name: 'Zrušit' }))

    expect(screen.queryByRole('group')).toBeNull()
    expect(removeProjectMember).not.toHaveBeenCalled()
  })

  it('disables all removal buttons while sending, then confirms and reloads the list', async () => {
    let resolve: () => void = () => {}
    vi.mocked(removeProjectMember).mockReturnValueOnce(new Promise<void>(done => { resolve = done }))
    renderMembers()

    await removeMember('Jan Novák')

    expect(vi.mocked(removeProjectMember).mock.calls[0].slice(0, 2)).toEqual([7, '2'])
    expect(within(row('Jan Novák')).getByRole<HTMLButtonElement>('button', { name: 'Odebíráme…' }).disabled).toBe(true)
    expect(within(row('Jan Novák')).getByRole<HTMLButtonElement>('button', { name: 'Zrušit' }).disabled).toBe(true)
    expect(within(row('Lucie Černá')).getByRole<HTMLButtonElement>('button', { name: 'Odebrat' }).disabled).toBe(true)

    await act(async () => resolve())

    expect(screen.getByRole('status').textContent).toBe('Jan Novák už není členem týmu.')
    expect(screen.queryByRole('group')).toBeNull()
    expect(within(row('Lucie Černá')).getByRole<HTMLButtonElement>('button', { name: 'Odebrat' }).disabled).toBe(false)
    expect(onChanged).toHaveBeenCalledTimes(1)
  })

  it.each([
    [404, 'MEMBER_NOT_FOUND', 'Tento uživatel není členem projektu.'],
    [409, 'LEADER_CANNOT_BE_REMOVED', 'Vedoucího týmu nelze z projektu odebrat.'],
    [403, 'PROJECT_LEADER_REQUIRED', 'Členy projektu může spravovat jen vedoucí týmu.'],
    [404, 'PROJECT_NOT_FOUND', 'Projekt neexistuje nebo k němu nemáte přístup.'],
  ])('shows %i %s and reloads, because the team changed meanwhile', async (status, code, message) => {
    vi.mocked(removeProjectMember).mockRejectedValueOnce(new ProjectMembersFailure(message, status, code))
    renderMembers()

    await removeMember('Jan Novák')

    expect(screen.getByRole('alert').textContent).toBe(message)
    expect(screen.queryByRole('group')).toBeNull()
    expect(onChanged).toHaveBeenCalledTimes(1)
  })

  it('reports a connection failure and reloads, because the member may have been removed', async () => {
    vi.mocked(removeProjectMember).mockRejectedValueOnce(new TypeError('Failed to fetch'))
    renderMembers()

    await removeMember('Jan Novák')

    expect(screen.getByRole('alert').textContent).toBe('Člena se nepodařilo odebrat. Zkontrolujte připojení a zkuste to znovu.')
    expect(onChanged).toHaveBeenCalledTimes(1)
  })

  it('lets the app re-check the session after 401', async () => {
    vi.mocked(removeProjectMember).mockRejectedValueOnce(new ProjectMembersFailure('Vaše přihlášení vypršelo.', 401, 'UNAUTHENTICATED'))
    renderMembers()

    await removeMember('Jan Novák')

    expect(refresh).toHaveBeenCalledTimes(1)
    expect(screen.queryByRole('alert')).toBeNull()
    expect(onChanged).not.toHaveBeenCalled()
  })

  it('clears the previous message when a new confirmation opens', async () => {
    vi.mocked(removeProjectMember).mockRejectedValueOnce(new TypeError('Failed to fetch'))
    renderMembers()
    await removeMember('Jan Novák')

    fireEvent.click(within(row('Lucie Černá')).getByRole('button', { name: 'Odebrat' }))

    expect(screen.queryByRole('alert')).toBeNull()
  })
})

describe('ProjectMembers messages', () => {
  it('shows only the result of the last action: removal replaces the add confirmation', async () => {
    vi.mocked(addProjectMember).mockResolvedValueOnce(members[2])
    vi.mocked(removeProjectMember).mockResolvedValueOnce(undefined)
    renderMembers()

    await addMember('lucie@example.test')
    expect(screen.getByRole('status').textContent).toBe('Lucie Černá je nyní členem týmu.')

    fireEvent.click(within(row('Lucie Černá')).getByRole('button', { name: 'Odebrat' }))
    expect(screen.queryByRole('status')).toBeNull()
    await act(async () => { fireEvent.click(within(row('Lucie Černá')).getByRole('button', { name: 'Odebrat' })) })

    expect(screen.getAllByRole('status')).toHaveLength(1)
    expect(screen.getByRole('status').textContent).toBe('Lucie Černá už není členem týmu.')
  })

  it('clears an old removal message as soon as the form is submitted', async () => {
    vi.mocked(removeProjectMember).mockRejectedValueOnce(new TypeError('Failed to fetch'))
    let resolve: (member: ProjectMember) => void = () => {}
    vi.mocked(addProjectMember).mockReturnValueOnce(new Promise(done => { resolve = done }))
    renderMembers()
    await removeMember('Jan Novák')
    expect(screen.getByRole('alert')).toBeTruthy()

    await addMember('lucie@example.test')

    expect(screen.queryByRole('alert')).toBeNull()
    await act(async () => resolve(members[2]))
    expect(screen.getByRole('status').textContent).toBe('Lucie Černá je nyní členem týmu.')
  })
})
