import { useKanbanColumns } from '../../../hooks/useKanbanColumns'
import { KanbanColumn } from '../column/KanbanColumn'
import './KanbanBoard.scss'

/// <summary>
/// Zobrazuje Kanban nástěnku.
/// </summary>
export function KanbanBoard() {
  const { columns, status } = useKanbanColumns()

  return (
    <section
      className="kanban-board"
      aria-labelledby="kanban-board-title"
      aria-busy={status === 'loading'}
    >
      <h2 id="kanban-board-title">Kanban</h2>
      {status === 'loading' && <p role="status">Načítání sloupců…</p>}
      {status === 'error' && (
        <p role="alert">Sloupce se nepodařilo načíst. Zkuste obnovit stránku.</p>
      )}
      <div className="kanban-columns">
        {columns.map((column) => (
          <KanbanColumn key={column.id} column={column} />
        ))}
      </div>
    </section>
  )
}
