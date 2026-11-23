// @vitest-environment jsdom
import { cleanup, fireEvent, render, screen, within } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { useProjectMembers } from '../../../hooks/projects/useProjectMembers'
import type { ProjectMembersState } from '../../../hooks/projects/UseProjectMembersTypes'
import type { ProjectMembers } from '../../../models/projects/ProjectMemberTypes'
import { ProjectSettingsPage } from './ProjectSettingsPage'

vi.mock('../../../hooks/projects/useProjectMembers', () => ({ useProjectMembers: vi.fn() }))
vi.mock('../../../auth/AuthContext', () => ({ useAuth: () => ({ refresh: vi.fn() }) }))

const team: ProjectMembers = {
  teamName: 'Tým Sova',
  members: [
    { userId: '1', displayName: 'Petra Svobodová', email: 'petra@example.test', isLeader: true, joinedAt: '2026-11-07T12:00:00Z' },
    { userId: '2', displayName: 'Jan Novák', email: 'jan@example.test', isLeader: false, joinedAt: '2026-11-07T12:30:00Z' },
  ],
}
const reload = vi.fn()

/// <summary>
/// Vykreslí stránku se zadaným stavem načtení a přihlášeným uživatelem.
/// </summary>
function renderPage(state: ProjectMembersState, currentUserId = '1') {
  vi.mocked(useProjectMembers).mockReturnValue({ state, reload })
  return render(<ProjectSettingsPage projectId={7} currentUserId={currentUserId} home={{ path: '/projects', label: 'Moje projekty' }} onNavigate={vi.fn()} />)
}

beforeEach(() => reload.mockReset())
afterEach(cleanup)

describe('ProjectSettingsPage', () => {
  it('shows a loading status for the given project', () => {
    renderPage({ status: 'loading' })

    expect(screen.getByRole('heading', { level: 2 }).textContent).toBe('Nastavení projektu')
    expect(screen.getByRole('status').textContent).toBe('Načítáme členy týmu…')
    expect(useProjectMembers).toHaveBeenCalledWith(7)
  })

  it('shows the leader the team name, member count and one row per member', () => {
    renderPage({ status: 'loaded', data: team })

    expect(screen.getByRole('heading', { level: 3 }).textContent).toBe('Členové týmu 2')
    expect(screen.getByText('Tým Sova')).toBeTruthy()
    const rows = screen.getAllByRole('listitem')
    expect(rows).toHaveLength(2)
    expect(within(rows[0]).getByText('Petra Svobodová (vy)')).toBeTruthy()
    expect(within(rows[0]).getByText('petra@example.test')).toBeTruthy()
    expect(within(rows[0]).getByText('Vedoucí')).toBeTruthy()
    expect(within(rows[0]).getByText('PS')).toBeTruthy()
    expect(within(rows[1]).getByText('Jan Novák')).toBeTruthy()
    expect(within(rows[1]).getByText('Člen')).toBeTruthy()
    expect(screen.queryByRole('alert')).toBeNull()
    expect(screen.getByRole('form', { name: 'Přidat člena' })).toBeTruthy()
  })

  it('tells a regular member that only the leader may change settings and hides the list', () => {
    renderPage({ status: 'loaded', data: team }, '2')

    expect(screen.getByRole('alert').textContent).toContain('Nemáte oprávnění')
    expect(screen.queryByRole('list')).toBeNull()
    expect(screen.queryByText('jan@example.test')).toBeNull()
    expect(screen.queryByRole('form')).toBeNull()
  })

  it('shows a missing or foreign project without a retry button', () => {
    renderPage({ status: 'error', message: 'Projekt neexistuje nebo k němu nemáte přístup.', notFound: true })

    expect(screen.getByRole('alert').textContent).toBe('Projekt neexistuje nebo k němu nemáte přístup.')
    expect(screen.queryByRole('button')).toBeNull()
  })

  it('offers a retry after a connection failure', () => {
    renderPage({ status: 'error', message: 'Členy projektu se nepodařilo načíst.', notFound: false })

    fireEvent.click(screen.getByRole('button', { name: 'Zkusit znovu' }))

    expect(reload).toHaveBeenCalledTimes(1)
  })
})
