/**
 * Projekt vrácený API pro hlavní dashboard.
 */
export type DashboardProject = {
  id: number
  name: string
  team: string
  membersCount: number
  role: string
  completed: number
  total: number
  openTasks: number
  overdueTasks: number
  milestone: string
  members: string[]
}

/**
 * Úkol vrácený API pro hlavní dashboard.
 */
export type DashboardTask = {
  id: number
  name: string
  project: string
  deadline: string
  priority: string
  status: string
  overdue: boolean
  completed: boolean
}

/**
 * Kompletní data dashboardu přihlášeného uživatele.
 */
export type DashboardData = {
  projects: DashboardProject[]
  tasks: DashboardTask[]
}

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === 'object' && value !== null
}

function isDashboardProject(value: unknown): value is DashboardProject {
  if (!isRecord(value)) return false

  return (
    typeof value.id === 'number' &&
    Number.isInteger(value.id) &&
    typeof value.name === 'string' &&
    typeof value.team === 'string' &&
    typeof value.membersCount === 'number' &&
    typeof value.role === 'string' &&
    typeof value.completed === 'number' &&
    typeof value.total === 'number' &&
    typeof value.openTasks === 'number' &&
    typeof value.overdueTasks === 'number' &&
    typeof value.milestone === 'string' &&
    Array.isArray(value.members) &&
    value.members.every((member) => typeof member === 'string')
  )
}

function isDashboardTask(value: unknown): value is DashboardTask {
  if (!isRecord(value)) return false

  return (
    typeof value.id === 'number' &&
    Number.isInteger(value.id) &&
    typeof value.name === 'string' &&
    typeof value.project === 'string' &&
    typeof value.deadline === 'string' &&
    typeof value.priority === 'string' &&
    typeof value.status === 'string' &&
    typeof value.overdue === 'boolean' &&
    typeof value.completed === 'boolean'
  )
}

function isDashboardData(value: unknown): value is DashboardData {
  if (!isRecord(value)) return false

  return (
    Array.isArray(value.projects) &&
    value.projects.every(isDashboardProject) &&
    Array.isArray(value.tasks) &&
    value.tasks.every(isDashboardTask)
  )
}

/**
 * Načte projekty a úkoly přihlášeného uživatele.
 */
export async function getDashboard(
  signal?: AbortSignal,
): Promise<DashboardData> {
  const response = await fetch('/api/dashboard', {
    signal,
    credentials: 'same-origin',
    cache: 'no-store',
  })

  if (!response.ok) {
    throw new Error(
      response.status === 401
        ? 'Přihlášení již není platné. Přihlaste se znovu.'
        : 'Nepodařilo se načíst přehled projektů.',
    )
  }

  const data: unknown = await response.json()

  if (!isDashboardData(data)) {
    throw new Error('API vrátilo neočekávaná data dashboardu.')
  }

  return data
}