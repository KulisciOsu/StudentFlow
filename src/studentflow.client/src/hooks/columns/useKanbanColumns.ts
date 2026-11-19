import { useEffect, useState } from 'react'
import { getColumns } from '../../services/columns/kanbanColumnsApi'
import type { KanbanColumnsState } from './UseKanbanColumnsTypes'

/**
 * Spravuje načítání sloupců nástěnky.
 */
export function useKanbanColumns(projectId: number): KanbanColumnsState {
  const [state, setState] = useState<KanbanColumnsState>({
    columns: [],
    status: 'loading',
  })

  useEffect(() => {
    const controller = new AbortController()
    let disposed = false
    const timeoutId = window.setTimeout(() => controller.abort(), 10000)

    async function loadColumns() {
      setState({ columns: [], status: 'loading' })

      try {
        const columns = await getColumns(projectId, controller.signal)

        if (!disposed) {
          setState({ columns, status: 'ready' })
        }
      } catch {
        if (!disposed) {
          setState({ columns: [], status: 'error' })
        }
      } finally {
        window.clearTimeout(timeoutId)
      }
    }

    void loadColumns()

    return () => {
      disposed = true
      window.clearTimeout(timeoutId)
      controller.abort()
    }
  }, [projectId])

  return state
}