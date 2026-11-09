
// @vitest-environment jsdom
import { act, renderHook, waitFor } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import type { Task } from '../../models/tasks/TaskTypes'
import { getTasks, saveTaskMove } from '../../services/tasks/kanbanTasksApi'
import { useKanbanTasks } from './useKanbanTasks'

vi.mock('../../services/tasks/kanbanTasksApi', () => ({
  getTasks: vi.fn(),
  saveTaskMove: vi.fn(),
}))

const projectId = 1

const tasks: Task[] = [
  { id: 1, title: 'První', columnId: 1, position: 0, priority: null, assignee: 'Jan Novák', dueDate: null },
  { id: 2, title: 'Druhý', columnId: 2, position: 0, priority: null, assignee: 'Jan Novák', dueDate: null },
]

/// <summary>
/// Vrátí sloupec úkolu.
/// </summary>
function columnOf(list: Task[], id: number) {
  return list.find((task) => task.id === id)?.columnId
}

/// <summary>
/// Vykreslí hook a počká na načtení úkolů.
/// </summary>
async function renderTasks() {
  const view = renderHook(() => useKanbanTasks(projectId))
  await waitFor(() => expect(view.result.current.status).toBe('ready'))
  return view
}

beforeEach(() => {
  vi.mocked(getTasks).mockReset().mockResolvedValue(tasks)
  vi.mocked(saveTaskMove).mockReset().mockResolvedValue(undefined)
})

describe('useKanbanTasks', () => {
  it('načte úkoly konkrétního projektu', async () => {
    const { result } = await renderTasks()

    expect(getTasks).toHaveBeenCalledWith(projectId, expect.any(AbortSignal))
    expect(result.current.tasks).toEqual(tasks)
  })

  it('při chybě načtení nastaví stav error', async () => {
    vi.mocked(getTasks).mockRejectedValue(new Error('chyba'))

    const { result } = renderHook(() => useKanbanTasks(projectId))

    await waitFor(() => expect(result.current.status).toBe('error'))
  })

  it('přesun uloží a ponechá kartu v novém sloupci', async () => {
    const { result } = await renderTasks()

    await act(() => result.current.moveTask(1, 2, 0))

    expect(saveTaskMove).toHaveBeenCalledWith(projectId, 1, 2, 0)
    expect(columnOf(result.current.tasks, 1)).toBe(2)
    expect(result.current.moveError).toBeNull()
  })

  it('při chybě uložení vrátí kartu a nabídne opakování', async () => {
    vi.mocked(saveTaskMove).mockRejectedValueOnce(new Error('chyba'))
    const { result } = await renderTasks()

    await act(() => result.current.moveTask(1, 2, 0))

    expect(columnOf(result.current.tasks, 1)).toBe(1)
    expect(result.current.moveError).toMatchObject({
      task: { id: 1, columnId: 1 },
      columnId: 2,
      position: 0,
    })

    await act(async () => result.current.retryMove())

    await waitFor(() => expect(columnOf(result.current.tasks, 1)).toBe(2))
    expect(saveTaskMove).toHaveBeenCalledTimes(2)
    expect(result.current.moveError).toBeNull()
  })

  it('přesun na stejné místo nevolá API', async () => {
    const { result } = await renderTasks()

    await act(() => result.current.moveTask(1, 1, 0))

    expect(saveTaskMove).not.toHaveBeenCalled()
  })
})
