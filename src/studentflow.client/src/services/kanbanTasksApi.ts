import type { Task } from '../models/TaskTypes'

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
}

/// <summary>
/// Načte úkoly nástěnky z API.
/// </summary>
export async function getTasks(signal?: AbortSignal): Promise<Task[]> {
  const response = await fetch('/api/tasks', { signal })

  if (!response.ok) {
    throw new Error('Nepodařilo se načíst úkoly.')
  }

  const tasks: unknown = await response.json()

  if (!Array.isArray(tasks) || !tasks.every(isTask)) {
    throw new Error('Neplatná odpověď API.')
  }

  return tasks
}
