import { useEffect, useState } from 'react'
import { getTasks } from '../services/kanbanTasksApi'
import type { KanbanTasksState } from './UseKanbanTasksTypes'

/// <summary>
/// Spravuje načítání úkolů nástěnky.
/// </summary>
export function useKanbanTasks(): KanbanTasksState {
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

  return state
}
