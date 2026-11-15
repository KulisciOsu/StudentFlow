// @vitest-environment jsdom
import { act, renderHook, waitFor } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import type { ProjectMembers } from '../../models/projects/ProjectMemberTypes'
import { getProjectMembers, ProjectMembersFailure } from '../../services/projects/projectMembersApi'
import { useProjectMembers } from './useProjectMembers'

const { refresh } = vi.hoisted(() => ({ refresh: vi.fn() }))

vi.mock('../../auth/AuthContext', () => ({ useAuth: () => ({ refresh }) }))
vi.mock('../../services/projects/projectMembersApi', async importOriginal => ({
  ...await importOriginal<typeof import('../../services/projects/projectMembersApi')>(),
  getProjectMembers: vi.fn(),
}))

const team: ProjectMembers = {
  teamName: 'Tým Sova',
  members: [
    { userId: '1', displayName: 'Petra Svobodová', email: 'petra@example.test', isLeader: true, joinedAt: '2026-11-07T12:00:00Z' },
    { userId: '2', displayName: 'Jan Novák', email: 'jan@example.test', isLeader: false, joinedAt: '2026-11-07T12:30:00Z' },
  ],
}

beforeEach(() => {
  refresh.mockReset().mockResolvedValue(undefined)
  vi.mocked(getProjectMembers).mockReset().mockResolvedValue(team)
})

describe('useProjectMembers', () => {
  it('starts loading and then returns the team of the given project', async () => {
    const { result } = renderHook(() => useProjectMembers(7))

    expect(result.current.state.status).toBe('loading')
    await waitFor(() => expect(result.current.state).toEqual({ status: 'loaded', data: team }))
    expect(vi.mocked(getProjectMembers).mock.calls[0][0]).toBe(7)
  })

  it('reloads on request and keeps the previous list visible meanwhile', async () => {
    const { result } = renderHook(() => useProjectMembers(7))
    await waitFor(() => expect(result.current.state.status).toBe('loaded'))
    const larger = { ...team, members: [...team.members, { ...team.members[1], userId: '3', displayName: 'Lucie Černá' }] }
    let resolveReload: (value: ProjectMembers) => void = () => {}
    vi.mocked(getProjectMembers).mockReturnValueOnce(new Promise(resolve => { resolveReload = resolve }))

    act(() => result.current.reload())

    expect(result.current.state).toEqual({ status: 'loaded', data: team })
    await act(async () => resolveReload(larger))
    expect(result.current.state).toEqual({ status: 'loaded', data: larger })
    expect(getProjectMembers).toHaveBeenCalledTimes(2)
  })

  it('reports a missing or foreign project as notFound with the API message', async () => {
    vi.mocked(getProjectMembers).mockRejectedValueOnce(
      new ProjectMembersFailure('Projekt neexistuje nebo k němu nemáte přístup.', 404, 'PROJECT_NOT_FOUND'))

    const { result } = renderHook(() => useProjectMembers(7))

    await waitFor(() => expect(result.current.state).toEqual({
      status: 'error', message: 'Projekt neexistuje nebo k němu nemáte přístup.', notFound: true }))
  })

  it('reports a network failure with a general message', async () => {
    vi.mocked(getProjectMembers).mockRejectedValueOnce(new TypeError('Failed to fetch'))

    const { result } = renderHook(() => useProjectMembers(7))

    await waitFor(() => expect(result.current.state).toEqual({
      status: 'error', message: 'Členy projektu se nepodařilo načíst. Zkontrolujte připojení a zkuste to znovu.', notFound: false }))
  })

  it('lets the app re-check the session after 401 instead of showing an error', async () => {
    vi.mocked(getProjectMembers).mockRejectedValueOnce(new ProjectMembersFailure('Vaše přihlášení vypršelo.', 401, 'UNAUTHENTICATED'))

    const { result } = renderHook(() => useProjectMembers(7))

    await waitFor(() => expect(refresh).toHaveBeenCalledTimes(1))
    expect(result.current.state.status).toBe('loading')
  })

  it('cancels the request when the page closes', async () => {
    const { unmount } = renderHook(() => useProjectMembers(7))
    const signal = vi.mocked(getProjectMembers).mock.calls[0][1]

    unmount()

    expect(signal?.aborted).toBe(true)
  })
})
