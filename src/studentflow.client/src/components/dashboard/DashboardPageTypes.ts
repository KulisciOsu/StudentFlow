import type {
  DashboardProject,
  DashboardTask,
} from '../../services/dashboard/dashboardApi'

/**
 * Datový model projektu zobrazovaného na dashboardu.
 */
export type Project = DashboardProject

/**
 * Datový model úkolu zobrazovaného na dashboardu.
 */
export type Task = DashboardTask

/**
 * Vstupní vlastnosti hlavní komponenty Dashboard.
 */
export type DashboardProps = {
  /** Otevře formulář pro založení nového projektu. */
  onCreateProject: () => void

  /** Otevře nástěnku konkrétního projektu. */
  onOpenProject: (projectId: number) => void

  /** Vloží dashboard pod společnou hlavičku aplikace. */
  embedded?: boolean
}

/**
 * Vstupní vlastnosti karty projektu.
 */
export type ProjectCardProps = {
  project: Project
  onOpen: (projectId: number) => void
}