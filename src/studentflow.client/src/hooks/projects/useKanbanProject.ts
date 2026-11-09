import { useEffect, useState } from 'react'
import { getProject } from '../../services/projects/kanbanProjectApi'
import type { KanbanProjectState } from './UseKanbanProjectTypes'

/**
 * Spravuje načítání projektu nástěnky.
 */
export function useKanbanProject(projectId: number): KanbanProjectState {
  const [state, setState] = useState<KanbanProjectState>({
    project: null,
    status: 'loading',
  })

  useEffect(() => {
    const controller = new AbortController()
    let disposed = false
    const timeoutId = window.setTimeout(() => controller.abort(), 10000)

    async function loadProject() {
      setState({ project: null, status: 'loading' })

      try {
        const project = await getProject(projectId, controller.signal)

        if (!disposed) {
          setState({ project, status: 'ready' })
        }
      } catch {
        if (!disposed) {
          setState({ project: null, status: 'error' })
        }
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
  }, [projectId])

  return state
}