import { useDroppable } from '@dnd-kit/core'
import { SortableContext, verticalListSortingStrategy } from '@dnd-kit/sortable'
import type { KanbanColumnProps } from './KanbanColumnTypes'
import { TaskCard } from '../task/TaskCard'
import './KanbanColumn.scss'

/// <summary>
/// Zobrazuje sloupec nástěnky.
/// </summary>
export function KanbanColumn({ column, tasks }: KanbanColumnProps) {
  const { setNodeRef, isOver } = useDroppable({ id: `column-${column.id}` })

  return (
    <section
      ref={setNodeRef}
      className={`kanban-column${isOver ? ' kanban-column--over' : ''}`}
      aria-labelledby={`column-${column.id}`}
    >
      <header className="kanban-column-header">
        <h3 id={`column-${column.id}`}>{column.title}</h3>
      </header>
      <SortableContext
        items={tasks.map((task) => `task-${task.id}`)}
        strategy={verticalListSortingStrategy}
      >
        <ul className="kanban-column-tasks">
          {tasks.map((task) => (
            <li key={task.id}>
              <TaskCard task={task} />
            </li>
          ))}
        </ul>
      </SortableContext>
    </section>
  )
}
