import { useKanbanColumns } from '../../../hooks/useKanbanColumns'
import { useKanbanTasks } from '../../../hooks/useKanbanTasks'
import { KanbanColumn } from '../column/KanbanColumn'
import './KanbanBoard.scss'

/// <summary>
/// Zobrazuje Kanban nástěnku.
/// </summary>
export function KanbanBoard() {
  const { columns, status: columnsStatus } = useKanbanColumns()
  const { tasks, status: tasksStatus } = useKanbanTasks()

  return (
    <section
      className="kanban-board"
      aria-labelledby="kanban-board-title"
      aria-busy={columnsStatus === 'loading' || tasksStatus === 'loading'}
    >
      <h2 id="kanban-board-title">Kanban</h2>
      {columnsStatus === 'loading' && <p role="status">Načítání sloupců…</p>}
      {columnsStatus === 'error' && (
        <p role="alert">Sloupce se nepodařilo načíst. Zkuste obnovit stránku.</p>
      )}
      {tasksStatus === 'loading' && <p role="status">Načítání úkolů…</p>}
      {tasksStatus === 'error' && (
        <p role="alert">Úkoly se nepodařilo načíst. Zkuste obnovit stránku.</p>
      )}
      <div className="kanban-columns">
        {columns.map((column) => (
          <KanbanColumn
            key={column.id}
            column={column}
            tasks={tasks.filter((task) => task.columnId === column.id)}
          />
        ))}
      </div>
    </section>
  )
}
