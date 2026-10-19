import { useEffect, useState } from 'react'
import { getColumns } from '../services/kanbanColumnsApi'
import type { KanbanColumnsState } from './UseKanbanColumnsTypes'

/// <summary>
/// Spravuje načítání sloupců nástěnky.
/// </summary>
export function useKanbanColumns(): KanbanColumnsState {
  const [state, setState] = useState<KanbanColumnsState>({
    columns: [],
    status: 'loading',
  })

  useEffect(() => {
    const controller = new AbortController()
    let disposed = false
    const timeoutId = window.setTimeout(() => controller.abort(), 10000)

    /// <summary>
    /// Načte sloupce a aktualizuje stav.
    /// </summary>
    async function loadColumns() {
      try {
        const columns = await getColumns(controller.signal)
        if (!disposed) setState({ columns, status: 'ready' })
      } catch {
        if (!disposed) setState({ columns: [], status: 'error' })
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
  }, [])

  return state
}
