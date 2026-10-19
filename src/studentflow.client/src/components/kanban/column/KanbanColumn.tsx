import type { KanbanColumnProps } from './KanbanColumnTypes'
import './KanbanColumn.scss'

/// <summary>
/// Zobrazuje záhlaví a prázdnou plochu jednoho sloupce nástěnky.
/// </summary>
export function KanbanColumn({ column }: KanbanColumnProps) {
  return (
    <section className="kanban-column" aria-labelledby={`column-${column.id}`}>
      <header className="kanban-column-header">
        <h3 id={`column-${column.id}`}>{column.title}</h3>
      </header>
    </section>
  )
}
