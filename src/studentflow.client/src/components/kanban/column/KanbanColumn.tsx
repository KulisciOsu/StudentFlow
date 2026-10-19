import type { KanbanColumnProps } from './KanbanColumnTypes'
import { TaskCard } from '../task/TaskCard'
import './KanbanColumn.scss'

/// <summary>
/// Zobrazuje sloupec nástěnky.
/// </summary>
export function KanbanColumn({ column, tasks }: KanbanColumnProps) {
  return (
    <section className="kanban-column" aria-labelledby={`column-${column.id}`}>
      <header className="kanban-column-header">
        <h3 id={`column-${column.id}`}>{column.title}</h3>
      </header>
      <ul className="kanban-column-tasks">
        {tasks.map((task) => (
          <li key={task.id}>
            <TaskCard task={task} />
          </li>
        ))}
      </ul>
    </section>
  )
}
