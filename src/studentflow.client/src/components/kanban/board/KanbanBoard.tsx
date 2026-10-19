import { useState } from 'react'
import {
  DndContext, DragOverlay, PointerSensor,
  pointerWithin, useSensor, useSensors,
} from '@dnd-kit/core'
import type { CollisionDetection, DragEndEvent, DragStartEvent } from '@dnd-kit/core'
import { useKanbanColumns } from '../../../hooks/columns/useKanbanColumns'
import { useKanbanTasks } from '../../../hooks/tasks/useKanbanTasks'
import { KanbanColumn } from '../column/KanbanColumn'
import './KanbanBoard.scss'

/// <summary>
/// Určí cíl přesunu karty.
/// </summary>
const detectCollision: CollisionDetection = (args) => {
  const collisions = pointerWithin(args)
  const cards = collisions.filter((collision) => String(collision.id).startsWith('task-'))
  return cards.length > 0 ? cards : collisions
}

/// <summary>
/// Zobrazuje Kanban nástěnku.
/// </summary>
export function KanbanBoard() {
  const { columns, status: columnsStatus } = useKanbanColumns()
  const { tasks, status: tasksStatus, moveTask } = useKanbanTasks()
  const [activeTaskId, setActiveTaskId] = useState<number | null>(null)
  const activeTask = tasks.find((task) => task.id === activeTaskId)
  const sensors = useSensors(
    useSensor(PointerSensor, { activationConstraint: { distance: 6 } }),
  )

  /// <summary>
  /// Zahájí přesun karty.
  /// </summary>
  function startDrag({ active }: DragStartEvent) {
    const task = tasks.find((item) => `task-${item.id}` === active.id)
    setActiveTaskId(task?.id ?? null)
  }

  /// <summary>
  /// Zruší přesun karty.
  /// </summary>
  function cancelDrag() {
    setActiveTaskId(null)
  }

  /// <summary>
  /// Aktualizuje sloupec a pozici karty.
  /// </summary>
  function finishDrag({ active, over }: DragEndEvent) {
    setActiveTaskId(null)
    if (!over || active.id === over.id) return

    const task = tasks.find((item) => `task-${item.id}` === active.id)
    const targetTask = tasks.find((item) => `task-${item.id}` === over.id)
    const targetColumn = columns.find((column) => `column-${column.id}` === over.id)
    const columnId = targetTask?.columnId ?? targetColumn?.id
    if (!task || columnId === undefined) return

    let position = targetTask?.position ?? tasks.filter((item) => item.columnId === columnId).length
    if (targetTask && task.columnId !== columnId) {
      const draggedRect = active.rect.current.translated
      if (draggedRect && draggedRect.top + draggedRect.height / 2 > over.rect.top + over.rect.height / 2) {
        position += 1
      }
    }

    moveTask(task.id, columnId, position)
  }

  return (
    <section
      className="kanban-board"
      aria-labelledby="kanban-board-title"
      aria-busy={columnsStatus === 'loading' || tasksStatus === 'loading'}
    >
      <h2 id="kanban-board-title">Kanban</h2>
      <p className="kanban-board-hint">
        Karty přesunete přetažením.
      </p>
      {columnsStatus === 'loading' && <p role="status">Načítání sloupců…</p>}
      {columnsStatus === 'error' && (
        <p role="alert">Sloupce se nepodařilo načíst. Zkuste obnovit stránku.</p>
      )}
      {tasksStatus === 'loading' && <p role="status">Načítání úkolů…</p>}
      {tasksStatus === 'error' && (
        <p role="alert">Úkoly se nepodařilo načíst. Zkuste obnovit stránku.</p>
      )}
      <DndContext
        sensors={sensors}
        collisionDetection={detectCollision}
        onDragStart={startDrag}
        onDragEnd={finishDrag}
        onDragCancel={cancelDrag}
      >
        <div className="kanban-columns">
          {columns.map((column) => (
            <KanbanColumn
              key={column.id}
              column={column}
              tasks={tasks.filter((task) => task.columnId === column.id)
                .sort((first, second) => first.position - second.position)}
            />
          ))}
        </div>
        <DragOverlay>
          {activeTask && (
            <article className="task-card task-card--overlay" aria-hidden="true">
              <h4>{activeTask.title}</h4>
            </article>
          )}
        </DragOverlay>
      </DndContext>
    </section>
  )
}
