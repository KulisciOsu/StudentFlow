import { useEffect, useState } from 'react'
import { getMyProjects } from '../../services/projects/myProjectsApi'
import type { MyProjectsResult } from './UseMyProjectsTypes'

/// <summary>
/// Spravuje načítání projektů a úkolů studenta.
/// </summary>
export function useMyProjects(): MyProjectsResult {
  const [state, setState] = useState<MyProjectsResult>({
    projects: [],
    tasks: [],
    status: 'loading',
  })

  useEffect(() => {
    const controller = new AbortController()
    let disposed = false
    const timeoutId = window.setTimeout(() => controller.abort(), 10000)

    /// <summary>
    /// Načte data rozcestníku a aktualizuje stav.
    /// </summary>
    async function load() {
      try {
        const data = await getMyProjects(controller.signal)
        if (!disposed) setState({ ...data, status: 'ready' })
      } catch {
        if (!disposed) setState({ projects: [], tasks: [], status: 'error' })
      } finally {
        window.clearTimeout(timeoutId)
      }
    }

    void load()

    return () => {
      disposed = true
      window.clearTimeout(timeoutId)
      controller.abort()
    }
  }, [])

  return state
}
