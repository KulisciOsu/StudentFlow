import type { Project } from '../../models/projects/ProjectTypes'

/**
 * Ověří data projektu vrácená API.
 */
function isProject(value: unknown): value is Project {
  return typeof value === 'object'
    && value !== null
    && 'id' in value
    && typeof value.id === 'number'
    && Number.isInteger(value.id)
    && 'name' in value
    && typeof value.name === 'string'
    && 'description' in value
    && typeof value.description === 'string'
    && 'teamName' in value
    && typeof value.teamName === 'string'
    && 'dueDate' in value
    && (value.dueDate === null || typeof value.dueDate === 'string')
}

/**
 * Načte projekt nástěnky podle jeho ID.
 */
export async function getProject(
  projectId: number,
  signal?: AbortSignal,
): Promise<Project> {
  const response = await fetch(`/api/projects/${projectId}`, {
    signal,
    credentials: 'same-origin',
  })

  if (!response.ok) {
    throw new Error('Nepodařilo se načíst projekt.')
  }

  const project: unknown = await response.json()

  if (!isProject(project)) {
    throw new Error('Neplatná odpověď API.')
  }

  return project
}