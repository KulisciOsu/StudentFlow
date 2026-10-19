import type { Column } from '../../../models/ColumnTypes'
import { KanbanColumn } from '../column/KanbanColumn'
import './KanbanBoard.scss'

/// <summary>
/// Výchozí sloupce nástěnky v pořadí jejich zobrazení.
/// </summary>
const columns: Column[] = [
  { id: 'todo', title: 'To Do' },
  { id: 'in-progress', title: 'In Progress' },
  { id: 'done', title: 'Done' },
]

/// <summary>
/// Zobrazuje Kanban nástěnku s výchozími prázdnými sloupci.
/// </summary>
export function KanbanBoard() {
  return (
    <section className="kanban-board" aria-labelledby="kanban-board-title">
      <h2 id="kanban-board-title">Kanban</h2>
      <div className="kanban-columns">
        {columns.map((column) => (
          <KanbanColumn key={column.id} column={column} />
        ))}
      </div>
    </section>
  )
}
