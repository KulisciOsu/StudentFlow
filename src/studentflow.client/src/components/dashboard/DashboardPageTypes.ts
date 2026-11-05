
/**
 * Datový model projektu zobrazovaného na dashboardu.
 */
export type Project = {
  /** Jedinečný identifikátor projektu. */
  id: number
  /** Název projektu. */
  name: string
  /** Název projektového týmu. */
  team: string
  /** Počet členů týmu. */
  membersCount: number
  /** Role aktuálního uživatele v projektu. */
  role: 'Vedoucí' | 'Člen'
  /** Počet dokončených úkolů. */
  completed: number
  /** Celkový počet úkolů. */
  total: number
  /** Počet otevřených úkolů. */
  openTasks: number
  /** Počet úkolů po termínu. */
  overdueTasks: number
  /** Aktuální milník projektu. */
  milestone: string
  /** Seznam členů týmu. */
  members: string[]
}

/**
 * Datový model úkolu zobrazovaného v přehledu dashboardu.
 */
export type Task = {
  /** Jedinečný identifikátor úkolu. */
  id: number
  /** Název úkolu. */
  name: string
  /** Projekt, ke kterému úkol patří. */
  project: string
  /** Termín dokončení úkolu. */
  deadline: string
  /** Priorita úkolu. */
  priority: 'Vysoká' | 'Střední'
  /** Aktuální stav úkolu. */
  status: string
  /** Označuje úkol po termínu. */
  overdue?: boolean
  /** Označuje dokončený úkol. */
  completed?: boolean
}

/**
 * Vstupní vlastnosti hlavní komponenty Dashboard.
 */
export type DashboardProps = {
  /** Otevře formulář pro založení nového projektu. */
  onCreateProject: () => void
  /** Vloží obsah pod společnou hlavičku aplikace bez ukázkového profilu a dalšího main. */
  embedded?: boolean
}

/**
 * Vstupní vlastnosti karty projektu.
 */
export type ProjectCardProps = {
  /** Projekt, jehož informace se zobrazí na kartě. */
  project: Project
}
