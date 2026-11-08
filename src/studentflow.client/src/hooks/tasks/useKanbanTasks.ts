import { useEffect, useState } from 'react'
import { getTasks, saveTaskMove } from '../../services/tasks/kanbanTasksApi'
import type { FailedTaskMove, KanbanTasksResult, KanbanTasksState } from './UseKanbanTasksTypes'
import { moveTask as moveTaskInBoard } from '../../utils/tasks/moveTask'

/// <summary>
/// Spravuje načítání úkolů nástěnky.
/// </summary>
export function useKanbanTasks(): KanbanTasksResult {
  const [state, setState] = useState<KanbanTasksState>({
    tasks: [],
    status: 'loading',
  })
  const [moveError, setMoveError] = useState<FailedTaskMove | null>(null)

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
  /// Přesune úkol a uloží změnu. Při chybě vrátí úkol na původní místo.
  /// </summary>
  async function moveTask(taskId: number, columnId: number, position: number) {
    const original = state.tasks.find((task) => task.id === taskId)
    if (!original || (original.columnId === columnId && original.position === position)) return

    setMoveError(null)
    setState((current) => ({
      ...current,
      tasks: moveTaskInBoard(current.tasks, taskId, columnId, position),
    }))

    try {
      await saveTaskMove(taskId, columnId, position)
    } catch {
      setState((current) => ({
        ...current,
        tasks: moveTaskInBoard(current.tasks, taskId, original.columnId, original.position),
      }))
      setMoveError({ task: original, columnId, position })
    }
  }

  /// <summary>
  /// Zopakuje neúspěšný přesun.
  /// </summary>
  function retryMove() {
    if (moveError) void moveTask(moveError.task.id, moveError.columnId, moveError.position)
  }

  return {
    ...state,
    moveError,
    moveTask,
    retryMove,
    dismissMoveError: () => setMoveError(null),
  }
}
