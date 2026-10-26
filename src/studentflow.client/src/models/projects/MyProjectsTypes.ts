/// <summary>
/// Role přihlášeného studenta v projektu.
/// </summary>
export type ProjectRole = 'leader' | 'member'

/// <summary>
/// Priorita úkolu.
/// </summary>
export type TaskPriority = 'high' | 'medium' | 'low'

/// <summary>
/// Stav úkolu.
/// </summary>
export type TaskStatus = 'todo' | 'inProgress' | 'done'

/// <summary>
/// Člen týmu zobrazený jako avatar.
/// </summary>
export interface ProjectMember {
  /// <summary>
  /// Identifikátor člena.
  /// </summary>
  id: number

  /// <summary>
  /// Jméno člena.
  /// </summary>
  name: string

  /// <summary>
  /// Iniciály člena.
  /// </summary>
  initials: string
}

/// <summary>
/// Nejbližší milník projektu.
/// </summary>
export interface ProjectMilestone {
  /// <summary>
  /// Název milníku.
  /// </summary>
  name: string

  /// <summary>
  /// Datum milníku ve formátu rrrr-mm-dd.
  /// </summary>
  date: string
}

/// <summary>
/// Projekt zobrazený na rozcestníku studenta.
/// </summary>
export interface ProjectSummary {
  /// <summary>
  /// Identifikátor projektu.
  /// </summary>
  id: number

  /// <summary>
  /// Název projektu.
  /// </summary>
  name: string

  /// <summary>
  /// Název týmu.
  /// </summary>
  teamName: string

  /// <summary>
  /// Role přihlášeného studenta v projektu.
  /// </summary>
  role: ProjectRole

  /// <summary>
  /// Počet hotových úkolů.
  /// </summary>
  tasksDone: number

  /// <summary>
  /// Počet všech úkolů.
  /// </summary>
  tasksTotal: number

  /// <summary>
  /// Počet otevřených úkolů přihlášeného studenta.
  /// </summary>
  myOpenTasks: number

  /// <summary>
  /// Počet úkolů přihlášeného studenta po termínu.
  /// </summary>
  myOverdueTasks: number

  /// <summary>
  /// Nejbližší milník nebo null, pokud žádný není.
  /// </summary>
  nextMilestone: ProjectMilestone | null

  /// <summary>
  /// Členové týmu.
  /// </summary>
  members: ProjectMember[]
}

/// <summary>
/// Úkol přihlášeného studenta napříč projekty.
/// </summary>
export interface MyTask {
  /// <summary>
  /// Identifikátor úkolu.
  /// </summary>
  id: number

  /// <summary>
  /// Název úkolu.
  /// </summary>
  title: string

  /// <summary>
  /// Identifikátor projektu úkolu.
  /// </summary>
  projectId: number

  /// <summary>
  /// Název projektu úkolu.
  /// </summary>
  projectName: string

  /// <summary>
  /// Termín úkolu ve formátu rrrr-mm-dd nebo null.
  /// </summary>
  deadline: string | null

  /// <summary>
  /// Priorita úkolu.
  /// </summary>
  priority: TaskPriority

  /// <summary>
  /// Stav úkolu.
  /// </summary>
  status: TaskStatus
}

/// <summary>
/// Data rozcestníku studenta.
/// </summary>
export interface MyProjectsData {
  /// <summary>
  /// Projekty, ve kterých je student členem nebo vedoucím.
  /// </summary>
  projects: ProjectSummary[]

  /// <summary>
  /// Úkoly studenta napříč projekty.
  /// </summary>
  tasks: MyTask[]
}
