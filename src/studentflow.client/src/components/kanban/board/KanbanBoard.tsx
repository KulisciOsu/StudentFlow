import { useState } from 'react'
import {
  DndContext, DragOverlay, PointerSensor,
  defaultDropAnimationSideEffects, pointerWithin, useSensor, useSensors,
} from '@dnd-kit/core'
import type { CollisionDetection, DragEndEvent, DragOverEvent, DragStartEvent } from '@dnd-kit/core'
import type { Task } from '../../../models/tasks/TaskTypes'
import { kanbanConfig } from '../kanbanConfig'
import { useKanbanColumns } from '../../../hooks/columns/useKanbanColumns'
import { useKanbanTasks } from '../../../hooks/tasks/useKanbanTasks'
import { moveTask as moveTaskInBoard } from '../../../utils/tasks/moveTask'
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
  const [previewTasks, setPreviewTasks] = useState<Task[] | null>(null)
  const displayedTasks = previewTasks ?? tasks
  const activeTask = displayedTasks.find((task) => task.id === activeTaskId)
  const sensors = useSensors(
    useSensor(PointerSensor, {
      activationConstraint: { distance: kanbanConfig.drag.activationDistance },
    }),
  )

  /// <summary>
  /// Zahájí přesun karty.
  /// </summary>
  function startDrag({ active }: DragStartEvent) {
    const task = tasks.find((item) => `task-${item.id}` === active.id)
    setPreviewTasks(tasks)
    setActiveTaskId(task?.id ?? null)
  }

  /// <summary>
  /// Zruší přesun karty.
  /// </summary>
  function cancelDrag() {
    setActiveTaskId(null)
    setPreviewTasks(null)
  }

  /// <summary>
  /// Určí cílový sloupec a pozici karty.
  /// </summary>
  function getDropTarget({ active, over }: DragOverEvent | DragEndEvent) {
    if (!over) return null

    const task = displayedTasks.find((item) => `task-${item.id}` === active.id)
    const targetTask = displayedTasks.find((item) => `task-${item.id}` === over.id)
    const targetColumn = columns.find((column) => `column-${column.id}` === over.id)
    const columnId = targetTask?.columnId ?? targetColumn?.id
    if (!task || columnId === undefined) return null

    let position = targetTask?.position ?? displayedTasks.filter((item) => item.columnId === columnId).length
    if (targetTask && task.columnId !== columnId) {
      const draggedRect = active.rect.current.translated
      if (draggedRect && draggedRect.top + draggedRect.height / 2 > over.rect.top + over.rect.height / 2) {
        position += 1
      }
    }

    return { task, columnId, position }
  }

  /// <summary>
  /// Zobrazí náhled přesunu do jiného sloupce.
  /// </summary>
  function previewDrag(event: DragOverEvent) {
    const target = getDropTarget(event)
    if (!target || target.task.columnId === target.columnId) return

    setPreviewTasks((current) => moveTaskInBoard(
      current ?? tasks, target.task.id, target.columnId, target.position,
    ))
  }

  /// <summary>
  /// Potvrdí přesun karty.
  /// </summary>
  function finishDrag(event: DragEndEvent) {
    const target = getDropTarget(event)
    setActiveTaskId(null)
    setPreviewTasks(null)

    if (target) moveTask(target.task.id, target.columnId, target.position)
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
        onDragOver={previewDrag}
        onDragEnd={finishDrag}
        onDragCancel={cancelDrag}
      >
        <div className="kanban-columns">
          {columns.map((column) => (
            <KanbanColumn
              key={column.id}
              column={column}
              tasks={displayedTasks.filter((task) => task.columnId === column.id)
                .sort((first, second) => first.position - second.position)}
            />
          ))}
        </div>
        <DragOverlay
          dropAnimation={{
            ...kanbanConfig.animation,
            sideEffects: defaultDropAnimationSideEffects({
              styles: { active: { opacity: '0' } },
            }),
          }}
        >
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
