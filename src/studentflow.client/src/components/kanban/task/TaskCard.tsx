import { useSortable } from '@dnd-kit/sortable'
import { CSS } from '@dnd-kit/utilities'
import { kanbanConfig } from '../kanbanConfig'
import type { TaskCardProps } from './TaskCardTypes'
import './TaskCard.scss'

/// <summary>
/// Zobrazuje kartu úkolu.
/// </summary>
export function TaskCard({ task }: TaskCardProps) {
  const {
    listeners, setNodeRef,
    transform, transition, isDragging,
  } = useSortable({
    id: `task-${task.id}`,
    transition: kanbanConfig.animation,
  })

  return (
    <article
      ref={setNodeRef}
      className={`task-card${isDragging ? ' task-card--dragging' : ''}`}
      style={{ transform: isDragging ? undefined : CSS.Transform.toString(transform), transition }}
      {...listeners}
      aria-labelledby={`task-${task.id}`}
    >
      <h4 id={`task-${task.id}`}>{task.title}</h4>
    </article>
  )
}
