import { useEffect, useState } from 'react'
import { getTasks, saveTaskMove } from '../../services/tasks/kanbanTasksApi'
import type {
  FailedTaskMove,
  KanbanTasksResult,
  KanbanTasksState,
} from './UseKanbanTasksTypes'
import { moveTask as moveTaskInBoard } from '../../utils/tasks/moveTask'

/**
 * Spravuje načítání úkolů nástěnky.
 */
export function useKanbanTasks(projectId: number): KanbanTasksResult {
  const [state, setState] = useState<KanbanTasksState>({
    tasks: [],
    status: 'loading',
  })

  const [moveError, setMoveError] = useState<FailedTaskMove | null>(null)

  useEffect(() => {
    const controller = new AbortController()
    let disposed = false
    const timeoutId = window.setTimeout(() => controller.abort(), 10000)

    async function loadTasks() {
      setState({ tasks: [], status: 'loading' })
      setMoveError(null)

      try {
        const tasks = await getTasks(projectId, controller.signal)

        if (!disposed) {
          setState({ tasks, status: 'ready' })
        }
      } catch {
        if (!disposed) {
          setState({ tasks: [], status: 'error' })
        }
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
  }, [projectId])

  /**
   * Přesune úkol a uloží změnu. Při chybě vrátí úkol na původní místo.
   */
  async function moveTask(taskId: number, columnId: number, position: number) {
    const original = state.tasks.find((task) => task.id === taskId)

    if (
      !original
      || (original.columnId === columnId && original.position === position)
    ) {
      return
    }

    setMoveError(null)

    setState((current) => ({
      ...current,
      tasks: moveTaskInBoard(current.tasks, taskId, columnId, position),
    }))

    try {
      await saveTaskMove(projectId, taskId, columnId, position)
    } catch {
      setState((current) => ({
        ...current,
        tasks: moveTaskInBoard(
          current.tasks,
          taskId,
          original.columnId,
          original.position,
        ),
      }))

      setMoveError({ task: original, columnId, position })
    }
  }

  /**
   * Zopakuje neúspěšný přesun.
   */
  function retryMove() {
    if (moveError) {
      void moveTask(
        moveError.task.id,
        moveError.columnId,
        moveError.position,
      )
    }
  }

  return {
    ...state,
    moveError,
    moveTask,
    retryMove,
    dismissMoveError: () => setMoveError(null),
  }
}