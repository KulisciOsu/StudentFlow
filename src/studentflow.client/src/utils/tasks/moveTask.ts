import type { Task } from '../../models/tasks/TaskTypes'

/// <summary>
/// Přesune úkol a přepočítá pozice ve sloupcích.
/// </summary>
export function moveTask(
  tasks: Task[], taskId: number, columnId: number, position: number,
): Task[] {
  const task = tasks.find((item) => item.id === taskId)
  if (!task || !Number.isInteger(position)) return tasks

  const remaining = tasks.filter((item) => item.id !== taskId)
  const destination = remaining.filter((item) => item.columnId === columnId)
    .sort((first, second) => first.position - second.position)
  const targetPosition = Math.max(0, Math.min(position, destination.length))
  destination.splice(targetPosition, 0, { ...task, columnId })

  const source = task.columnId === columnId ? [] : remaining
    .filter((item) => item.columnId === task.columnId)
    .sort((first, second) => first.position - second.position)

  return [
    ...remaining.filter((item) => item.columnId !== task.columnId && item.columnId !== columnId),
    ...source.map((item, index) => ({ ...item, position: index })),
    ...destination.map((item, index) => ({ ...item, position: index })),
  ]
}
