import { useSortable } from '@dnd-kit/sortable'
import { CSS } from '@dnd-kit/utilities'
import { isOverdue } from '../../../utils/tasks/taskDisplay'
import { kanbanConfig } from '../kanbanConfig'
import { TaskCardContent } from './TaskCardContent'
import type { TaskCardProps } from './TaskCardTypes'
import './TaskCard.scss'

/// <summary>
/// Zobrazuje kartu úkolu.
/// </summary>
export function TaskCard({ task, isDone }: TaskCardProps) {
  const {
    listeners, setNodeRef,
    transform, transition, isDragging,
  } = useSortable({
    id: `task-${task.id}`,
    transition: kanbanConfig.animation,
    disabled: !task.canMove,
  })

  const classNames = ['task-card']
  if (isDragging) classNames.push('task-card--dragging')
  if (!task.canMove) classNames.push('task-card--locked')
  if (isDone) classNames.push('task-card--done')
  else if (isOverdue(task.dueDate)) classNames.push('task-card--overdue')

  return (
    <article
      ref={setNodeRef}
      className={classNames.join(' ')}
      style={{ transform: isDragging ? undefined : CSS.Transform.toString(transform), transition }}
      {...listeners}
    >
      <TaskCardContent task={task} isDone={isDone} />
    </article>
  )
}
