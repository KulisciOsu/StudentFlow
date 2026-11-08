import { formatShortDate, getInitials, isOverdue, priorityLabels } from '../../../utils/tasks/taskDisplay'
import type { TaskCardProps } from './TaskCardTypes'

/// <summary>
/// Zobrazuje obsah karty úkolu.
/// </summary>
export function TaskCardContent({ task, isDone }: TaskCardProps) {
  const overdue = !isDone && isOverdue(task.dueDate)

  return (
    <>
      <div className="task-card-top">
        <h4>{task.title}</h4>
        {task.priority && !isDone && (
          <span className={`task-card-priority task-card-priority--${task.priority}`}>
            {priorityLabels[task.priority]}
          </span>
        )}
      </div>
      <div className="task-card-meta">
        <span className="task-card-avatar" title={task.assignee}>{getInitials(task.assignee)}</span>
        {task.dueDate && (
          <span className={overdue ? 'task-card-date task-card-date--overdue' : 'task-card-date'}>
            {isDone ? 'Hotovo ' : overdue ? 'Po termínu · ' : ''}
            {formatShortDate(task.dueDate)}
          </span>
        )}
      </div>
    </>
  )
}
