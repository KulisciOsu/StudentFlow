import type { Column } from '../../models/columns/ColumnTypes'
import type { Project } from '../../models/projects/ProjectTypes'
import type { Task } from '../../models/tasks/TaskTypes'
import { moveTask } from '../../utils/tasks/moveTask'

/// <summary>
/// Simulovaná odezva serveru v milisekundách.
/// </summary>
const delayMs = 300

/// <summary>
/// Při true každé uložení přesunu selže; slouží k vyzkoušení chybového stavu.
/// </summary>
const failMoves = false

const project: Project = {
  id: 1,
  name: 'Rezervační systém knihovny',
  teamName: 'Tým Sova',
  dueDate: '2027-01-15',
}

const columns: Column[] = [
  { id: 1, title: 'K řešení', isDone: false },
  { id: 2, title: 'Rozpracováno', isDone: false },
  { id: 3, title: 'Ke kontrole', isDone: false },
  { id: 4, title: 'Hotovo', isDone: true },
]

let tasks: Task[] = [
  { id: 1, title: 'Testovací scénáře přihlášení', columnId: 1, position: 0, priority: 'medium', assignee: 'Lucie Černá', dueDate: '2026-11-12' },
  { id: 2, title: 'Formulář rezervace', columnId: 1, position: 1, priority: 'high', assignee: 'Tomáš Dvořák', dueDate: '2026-11-20' },
  { id: 3, title: 'Export výpůjček do CSV', columnId: 1, position: 2, priority: 'low', assignee: 'Martin Kučera', dueDate: '2026-12-04' },
  { id: 4, title: 'Návrh databázového modelu', columnId: 2, position: 0, priority: 'high', assignee: 'Jan Novák', dueDate: '2026-10-22' },
  { id: 5, title: 'API pro výpůjčky', columnId: 2, position: 1, priority: 'medium', assignee: 'Martin Kučera', dueDate: '2026-11-05' },
  { id: 6, title: 'Wireframy obrazovek', columnId: 2, position: 2, priority: 'medium', assignee: 'Petra Svobodová', dueDate: '2026-11-09' },
  { id: 7, title: 'Diagram případů užití', columnId: 3, position: 0, priority: 'medium', assignee: 'Lucie Černá', dueDate: '2026-11-06' },
  { id: 8, title: 'Vize projektu', columnId: 4, position: 0, priority: null, assignee: 'Petra Svobodová', dueDate: '2026-10-10' },
  { id: 9, title: 'Popis problému', columnId: 4, position: 1, priority: null, assignee: 'Jan Novák', dueDate: '2026-10-12' },
  { id: 10, title: 'Risk-list', columnId: 4, position: 2, priority: null, assignee: 'Lucie Černá', dueDate: '2026-10-15' },
]

/// <summary>
/// Počká na simulovanou odezvu serveru.
/// </summary>
function wait(): Promise<void> {
  return new Promise((resolve) => setTimeout(resolve, delayMs))
}

/// <summary>
/// Ukázková data nástěnky, dokud není hotový backend.
/// </summary>
export const kanbanMockApi = {
  async getProject(): Promise<Project> {
    await wait()
    return { ...project }
  },

  async getColumns(): Promise<Column[]> {
    await wait()
    return columns.map((column) => ({ ...column }))
  },

  async getTasks(): Promise<Task[]> {
    await wait()
    return tasks.map((task) => ({ ...task }))
  },

  async saveTaskMove(taskId: number, columnId: number, position: number): Promise<void> {
    await wait()
    if (failMoves) throw new Error('Simulovaná chyba uložení.')
    tasks = moveTask(tasks, taskId, columnId, position)
  },
}
