// @vitest-environment jsdom
import { renderHook, waitFor } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import type { Project } from '../../models/projects/ProjectTypes'
import { getProject, ProjectForbiddenError } from '../../services/projects/kanbanProjectApi'
import { useKanbanProject } from './useKanbanProject'

vi.mock('../../services/projects/kanbanProjectApi', async (importOriginal) => ({
  ...await importOriginal<typeof import('../../services/projects/kanbanProjectApi')>(),
  getProject: vi.fn(),
}))

const project: Project = { id: 1, name: 'Projekt', teamName: 'Tým', dueDate: null, isReadOnly: true }

beforeEach(() => {
  vi.mocked(getProject).mockReset()
})

describe('useKanbanProject', () => {
  it('načte projekt včetně režimu jen pro čtení', async () => {
    vi.mocked(getProject).mockResolvedValue(project)

    const { result } = renderHook(() => useKanbanProject())

    await waitFor(() => expect(result.current.status).toBe('ready'))
    expect(result.current.project?.isReadOnly).toBe(true)
  })

  it('bez přístupu k projektu nastaví stav forbidden', async () => {
    vi.mocked(getProject).mockRejectedValue(new ProjectForbiddenError())

    const { result } = renderHook(() => useKanbanProject())

    await waitFor(() => expect(result.current.status).toBe('forbidden'))
  })

  it('při jiné chybě nastaví stav error', async () => {
    vi.mocked(getProject).mockRejectedValue(new Error('chyba'))

    const { result } = renderHook(() => useKanbanProject())

    await waitFor(() => expect(result.current.status).toBe('error'))
  })
})
