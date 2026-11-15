import type { Project } from '../../models/projects/ProjectTypes'
import { kanbanConfig } from '../../components/kanban/kanbanConfig'

/// <summary>
/// Ověří data projektu.
/// </summary>
function isProject(value: unknown): value is Project {
  return typeof value === 'object'
    && value !== null
    && 'id' in value
    && typeof value.id === 'number'
    && 'name' in value
    && typeof value.name === 'string'
    && 'teamName' in value
    && typeof value.teamName === 'string'
    && 'dueDate' in value
    && (value.dueDate === null || typeof value.dueDate === 'string')
    && 'isReadOnly' in value
    && typeof value.isReadOnly === 'boolean'
}

/// <summary>
/// Chyba, když přihlášený uživatel nemá k projektu přístup.
/// </summary>
export class ProjectForbiddenError extends Error {
  constructor() {
    super('Do projektu nemáte přístup.')
    this.name = 'ProjectForbiddenError'
  }
}

/// <summary>
/// Načte projekt nástěnky z API.
/// </summary>
export async function getProject(signal?: AbortSignal): Promise<Project> {
  const response = await fetch(`/api/projects/${kanbanConfig.projectId}`, { signal })

  if (response.status === 403) {
    throw new ProjectForbiddenError()
  }

  if (!response.ok) {
    throw new Error('Nepodařilo se načíst projekt.')
  }

  const project: unknown = await response.json()

  if (!isProject(project)) {
    throw new Error('Neplatná odpověď API.')
  }

  return project
}
