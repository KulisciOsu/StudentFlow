import type { Task } from '../../models/tasks/TaskTypes'
import { kanbanConfig } from '../../components/kanban/kanbanConfig'

/// <summary>
/// Ověří data úkolu.
/// </summary>
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
    && 'isMine' in value
    && typeof value.isMine === 'boolean'
    && 'canMove' in value
    && typeof value.canMove === 'boolean'
}

/// <summary>
/// Načte úkoly nástěnky z API.
/// </summary>
export async function getTasks(signal?: AbortSignal): Promise<Task[]> {
  const response = await fetch(`/api/projects/${kanbanConfig.projectId}/tasks`, { signal })

  if (!response.ok) {
    throw new Error('Nepodařilo se načíst úkoly.')
  }

  const tasks: unknown = await response.json()

  if (!Array.isArray(tasks) || !tasks.every(isTask)) {
    throw new Error('Neplatná odpověď API.')
  }

  return tasks
}

/// <summary>
/// Uloží nový sloupec a pozici úkolu.
/// </summary>
export async function saveTaskMove(taskId: number, columnId: number, position: number): Promise<void> {
  const response = await fetch(`/api/projects/${kanbanConfig.projectId}/tasks/${taskId}/position`, {
    method: 'PUT',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ columnId, position }),
  })

  if (!response.ok) {
    throw new Error('Nepodařilo se uložit přesun úkolu.')
  }
}
