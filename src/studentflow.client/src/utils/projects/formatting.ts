import type { MyTask } from '../../models/projects/MyProjectsTypes'
import { toIsoDate } from './dates'

/// <summary>
/// Vybere správný tvar slova podle počtu v češtině (1 / 2–4 / 5 a více).
/// </summary>
export function pluralCs(count: number, forms: readonly [string, string, string]): string {
  if (count === 1) return forms[0]
  if (count >= 2 && count <= 4) return forms[1]
  return forms[2]
}

/// <summary>
/// Zkrátí datum rrrr-mm-dd na tvar „d. m.“.
/// </summary>
export function formatShortDate(isoDate: string): string {
  const [, month, day] = isoDate.split('-').map(Number)
  return `${day}. ${month}.`
}

/// <summary>
/// Zjistí, zda je otevřený úkol po termínu.
/// </summary>
export function isOverdue(task: MyTask, today: Date = new Date()): boolean {
  return task.status !== 'done' && task.deadline !== null && task.deadline < toIsoDate(today)
}
