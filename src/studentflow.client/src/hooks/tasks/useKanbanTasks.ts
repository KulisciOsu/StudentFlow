import { useEffect, useState } from 'react'
import { getTasks } from '../../services/tasks/kanbanTasksApi'
import type { KanbanTasksResult, KanbanTasksState } from './UseKanbanTasksTypes'
import { moveTask as moveTaskInBoard } from '../../utils/tasks/moveTask'

/// <summary>
/// Spravuje načítání úkolů nástěnky.
/// </summary>
export function useKanbanTasks(): KanbanTasksResult {
  const [state, setState] = useState<KanbanTasksState>({
    tasks: [],
    status: 'loading',
  })

  useEffect(() => {
    const controller = new AbortController()
    let disposed = false
    const timeoutId = window.setTimeout(() => controller.abort(), 10000)

    /// <summary>
    /// Načte úkoly a aktualizuje stav.
    /// </summary>
    async function loadTasks() {
      try {
        const tasks = await getTasks(controller.signal)
        if (!disposed) setState({ tasks, status: 'ready' })
      } catch {
        if (!disposed) setState({ tasks: [], status: 'error' })
      } finally {
        window.clearTimeout(timeoutId)
      }
    }

    void loadTasks()

    return () => {
      disposed = true
      window.clearTimeout(timeoutId)
      controller.abort()
    }
  }, [])

  /// <summary>
  /// Aktualizuje sloupec a pozici úkolu.
  /// </summary>
  function moveTask(taskId: number, columnId: number, position: number) {
    setState((current) => ({
      ...current,
      tasks: moveTaskInBoard(current.tasks, taskId, columnId, position),
    }))
  }

  return { ...state, moveTask }
}
