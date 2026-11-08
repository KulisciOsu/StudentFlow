import { isRecord } from '../../auth/types'
import type { ProjectMember, ProjectMembers } from '../../models/projects/ProjectMemberTypes'
import { csrfRequest } from '../http/csrfRequest'

/// <summary>
/// Zprávy pro uživatele podle kódu chyby z API členů projektu.
/// </summary>
const errorMessages = new Map<string, string>([
  ['VALIDATION_FAILED', 'Zadejte platnou e-mailovou adresu, nejvýše 320 znaků.'],
  ['UNAUTHENTICATED', 'Vaše přihlášení vypršelo. Přihlaste se prosím znovu.'],
  ['CSRF_INVALID', 'Ověření požadavku se nepodařilo. Zkuste to znovu.'],
  ['PROJECT_LEADER_REQUIRED', 'Členy projektu může spravovat jen vedoucí týmu.'],
  ['PROJECT_NOT_FOUND', 'Projekt neexistuje nebo k němu nemáte přístup.'],
  ['MEMBER_NOT_FOUND', 'Tento uživatel není členem projektu.'],
  ['MEMBER_ALREADY_EXISTS', 'Tento uživatel už je členem týmu.'],
  ['LEADER_CANNOT_BE_REMOVED', 'Vedoucího týmu nelze z projektu odebrat.'],
  ['ACCOUNT_NOT_FOUND', 'Účet s tímto e-mailem v StudentFlow neexistuje.'],
  ['ACCOUNT_NOT_STUDENT', 'Do týmu lze přidat jen účet studenta.'],
])

const invalidResponseMessage = 'Odpověď serveru se nepodařilo zpracovat. Obnovte stránku.'

/// <summary>
/// Chyba API členů projektu se zprávou pro uživatele, HTTP statusem a kódem chyby z odpovědi.
/// </summary>
export class ProjectMembersFailure extends Error {
  /// <summary>
  /// HTTP status odpovědi; podle 401 stránka obnoví přihlášení.
  /// </summary>
  readonly status: number

  /// <summary>
  /// Kód chyby z ProblemDetails, nebo null, pokud ho odpověď neobsahovala.
  /// </summary>
  readonly code: string | null

  constructor(message: string, status: number, code: string | null = null) {
    super(message)
    this.name = 'ProjectMembersFailure'
    this.status = status
    this.code = code
  }
}

/// <summary>
/// Načte název týmu a aktuální členy projektu; smí každý člen projektu.
/// </summary>
export async function getProjectMembers(projectId: number, signal?: AbortSignal): Promise<ProjectMembers> {
  const response = await fetch(membersPath(projectId), { signal, credentials: 'same-origin', cache: 'no-store' })
  if (!response.ok) throw await toFailure(response, 'Členy projektu se nepodařilo načíst.')

  const value: unknown = await response.json().catch(() => null)
  if (!isProjectMembers(value)) throw new ProjectMembersFailure(invalidResponseMessage, response.status)
  return value
}

/// <summary>
/// Přidá do projektu registrovaného studenta podle e-mailu; smí pouze vedoucí týmu.
/// </summary>
export async function addProjectMember(projectId: number, email: string, signal?: AbortSignal): Promise<ProjectMember> {
  const response = await csrfRequest('POST', membersPath(projectId), { email: email.trim() }, signal)
  if (!response.ok) throw await toFailure(response, 'Člena se nepodařilo přidat.')

  const value: unknown = await response.json().catch(() => null)
  if (!isRecord(value) || !isProjectMember(value.member))
    throw new ProjectMembersFailure(invalidResponseMessage, response.status)
  return value.member
}

/// <summary>
/// Odebere běžného člena z projektu; smí pouze vedoucí týmu a vedoucího odebrat nelze.
/// </summary>
export async function removeProjectMember(projectId: number, userId: string, signal?: AbortSignal): Promise<void> {
  const response = await csrfRequest('DELETE', `${membersPath(projectId)}/${encodeURIComponent(userId)}`, undefined, signal)
  if (!response.ok) throw await toFailure(response, 'Člena se nepodařilo odebrat.')
}

/// <summary>
/// Adresa API členů daného projektu.
/// </summary>
function membersPath(projectId: number): string {
  return `/api/projects/${projectId}/members`
}

/// <summary>
/// Převede chybovou odpověď na zprávu podle kódu; neznámý kód použije předaný obecný text.
/// </summary>
async function toFailure(response: Response, fallback: string): Promise<ProjectMembersFailure> {
  const problem: unknown = await response.json().catch(() => null)
  const code = isRecord(problem) && typeof problem.code === 'string' ? problem.code : null
  const message = errorMessages.get(code ?? (response.status === 401 ? 'UNAUTHENTICATED' : '')) ?? fallback
  return new ProjectMembersFailure(message, response.status, code)
}

/// <summary>
/// Ověří data jednoho člena projektu.
/// </summary>
function isProjectMember(value: unknown): value is ProjectMember {
  return isRecord(value)
    && typeof value.userId === 'string' && /^[1-9][0-9]*$/.test(value.userId)
    && typeof value.displayName === 'string' && value.displayName.trim().length > 0
    && typeof value.email === 'string' && value.email.length > 0
    && typeof value.isLeader === 'boolean'
    && typeof value.addedAt === 'string' && Number.isFinite(Date.parse(value.addedAt))
}

/// <summary>
/// Ověří odpověď se seznamem členů a názvem týmu.
/// </summary>
function isProjectMembers(value: unknown): value is ProjectMembers {
  return isRecord(value)
    && typeof value.teamName === 'string'
    && Array.isArray(value.members)
    && value.members.every(isProjectMember)
}
