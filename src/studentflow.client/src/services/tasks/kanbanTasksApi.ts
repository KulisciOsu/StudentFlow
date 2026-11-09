import type { Task } from '../../models/tasks/TaskTypes'

/**
 * Ověří data úkolu.
 */
function isTask(value: unknown): value is Task {
  return typeof value === 'object'
    && value !== null
    && 'id' in value
    && typeof value.id === 'number'
    && Number.isInteger(value.id)
    && 'title' in value
    && typeof value.title === 'string'
    && 'columnId' in value
    && typeof value.columnId === 'number'
    && Number.isInteger(value.columnId)
    && 'position' in value
    && typeof value.position === 'number'
    && Number.isInteger(value.position)
    && value.position >= 0
    && 'priority' in value
    && (value.priority === null || ['low', 'medium', 'high'].includes(value.priority as string))
    && 'assignee' in value
    && typeof value.assignee === 'string'
    && 'dueDate' in value
    && (value.dueDate === null || typeof value.dueDate === 'string')
}

/**
 * Načte úkoly nástěnky podle ID projektu.
 */
export async function getTasks(
  projectId: number,
  signal?: AbortSignal,
): Promise<Task[]> {
  const response = await fetch(`/api/projects/${projectId}/tasks`, {
    signal,
    credentials: 'same-origin',
  })

  if (!response.ok) {
    throw new Error('Nepodařilo se načíst úkoly.')
  }

  const tasks: unknown = await response.json()

  if (!Array.isArray(tasks) || !tasks.every(isTask)) {
    throw new Error('Neplatná odpověď API.')
  }

  return tasks
}

/**
 * Uloží nový sloupec a pozici úkolu v konkrétním projektu.
 */
export async function saveTaskMove(
  projectId: number,
  taskId: number,
  columnId: number,
  position: number,
): Promise<void> {
  const response = await fetch(
    `/api/projects/${projectId}/tasks/${taskId}/position`,
    {
      method: 'PUT',
      credentials: 'same-origin',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ columnId, position }),
    },
  )

  if (!response.ok) {
    throw new Error('Nepodařilo se uložit přesun úkolu.')
  }
}