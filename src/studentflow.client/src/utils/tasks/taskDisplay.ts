import type { TaskPriority } from '../../models/tasks/TaskTypes'

/// <summary>
/// České názvy priorit.
/// </summary>
export const priorityLabels: Record<TaskPriority, string> = {
  high: 'Vysoká',
  medium: 'Střední',
  low: 'Nízká',
}

/// <summary>
/// Zformátuje datum RRRR-MM-DD jako „22. 10.“.
/// </summary>
export function formatShortDate(date: string): string {
  const [, month, day] = date.split('-').map(Number)
  return `${day}. ${month}.`
}

/// <summary>
/// Zformátuje datum RRRR-MM-DD jako „15. 1. 2027“.
/// </summary>
export function formatDate(date: string): string {
  return `${formatShortDate(date)} ${date.slice(0, 4)}`
}

/// <summary>
/// Určí, zda termín již uplynul.
/// </summary>
export function isOverdue(dueDate: string | null): boolean {
  if (!dueDate) return false
  const today = new Date()
  const month = String(today.getMonth() + 1).padStart(2, '0')
  const day = String(today.getDate()).padStart(2, '0')
  return dueDate < `${today.getFullYear()}-${month}-${day}`
}

/// <summary>
/// Vrátí iniciály jména, např. „Lucie Černá“ → „LČ“.
/// </summary>
export function getInitials(name: string): string {
  const parts = name.trim().split(/\s+/)
  return `${parts[0]?.[0] ?? ''}${parts.length > 1 ? parts[parts.length - 1][0] : ''}`.toUpperCase()
}
