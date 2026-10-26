import type {
  CreatedProject, CreateProjectErrors, CreateProjectField, CreateProjectRequest,
} from '../../models/projects/ProjectTypes'

/// <summary>
/// Chyba validace vrácená API s hláškami podle polí.
/// </summary>
export class CreateProjectValidationError extends Error {
  /// <summary>
  /// Hlášky podle polí formuláře.
  /// </summary>
  readonly fieldErrors: CreateProjectErrors

  constructor(fieldErrors: CreateProjectErrors) {
    super('Údaje projektu nejsou platné.')
    this.name = 'CreateProjectValidationError'
    this.fieldErrors = fieldErrors
  }
}

const fields: readonly CreateProjectField[] = ['name', 'description', 'deadline', 'teamName']

/// <summary>
/// Ověří data založeného projektu.
/// </summary>
function isCreatedProject(value: unknown): value is CreatedProject {
  return typeof value === 'object'
    && value !== null
    && 'id' in value
    && typeof value.id === 'number'
    && Number.isInteger(value.id)
    && 'name' in value
    && typeof value.name === 'string'
    && 'teamId' in value
    && typeof value.teamId === 'number'
    && Number.isInteger(value.teamId)
}

/// <summary>
/// Přečte chyby polí z odpovědi ASP.NET Core (ValidationProblemDetails).
/// </summary>
async function readFieldErrors(response: Response): Promise<CreateProjectErrors> {
  const fieldErrors: CreateProjectErrors = {}

  try {
    const body: unknown = await response.json()
    if (typeof body !== 'object' || body === null || !('errors' in body)) return fieldErrors

    const errors = body.errors
    if (typeof errors !== 'object' || errors === null) return fieldErrors

    for (const [key, messages] of Object.entries(errors)) {
      const field = fields.find((item) => item.toLowerCase() === key.toLowerCase())
      if (field && Array.isArray(messages) && typeof messages[0] === 'string') {
        fieldErrors[field] = messages[0]
      }
    }
  } catch {
    // Tělo odpovědi není platné JSON, zobrazí se obecná chyba.
  }

  return fieldErrors
}

/// <summary>
/// Založí projekt společně s týmem.
/// </summary>
export async function createProject(
  request: CreateProjectRequest,
  signal?: AbortSignal,
): Promise<CreatedProject> {
  const response = await fetch('/api/projects', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(request),
    signal,
  })

  if (response.status === 400) {
    const fieldErrors = await readFieldErrors(response)
    if (Object.keys(fieldErrors).length > 0) throw new CreateProjectValidationError(fieldErrors)
  }

  if (!response.ok) {
    throw new Error('Nepodařilo se založit projekt.')
  }

  const project: unknown = await response.json()

  if (!isCreatedProject(project)) {
    throw new Error('Neplatná odpověď API.')
  }

  return project
}
