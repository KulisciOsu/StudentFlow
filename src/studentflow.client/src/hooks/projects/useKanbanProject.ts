import { useEffect, useState } from 'react'
import { getProject } from '../../services/projects/kanbanProjectApi'
import type { KanbanProjectState } from './UseKanbanProjectTypes'

/// <summary>
/// Spravuje načítání projektu nástěnky.
/// </summary>
export function useKanbanProject(): KanbanProjectState {
  const [state, setState] = useState<KanbanProjectState>({
    project: null,
    status: 'loading',
  })

  useEffect(() => {
    const controller = new AbortController()
    let disposed = false
    const timeoutId = window.setTimeout(() => controller.abort(), 10000)

    /// <summary>
    /// Načte projekt a aktualizuje stav.
    /// </summary>
    async function loadProject() {
      try {
        const project = await getProject(controller.signal)
        if (!disposed) setState({ project, status: 'ready' })
      } catch {
        if (!disposed) setState({ project: null, status: 'error' })
      } finally {
        window.clearTimeout(timeoutId)
      }
    }

    void loadProject()

    return () => {
      disposed = true
      window.clearTimeout(timeoutId)
      controller.abort()
    }
  }, [])

  return state
}
