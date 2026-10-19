import type { TaskCardProps } from './TaskCardTypes'
import './TaskCard.scss'

/// <summary>
/// Zobrazuje kartu úkolu.
/// </summary>
export function TaskCard({ task }: TaskCardProps) {
  return (
    <article className="task-card" aria-labelledby={`task-${task.id}`}>
      <h4 id={`task-${task.id}`}>{task.title}</h4>
    </article>
  )
}
