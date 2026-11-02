import { useState } from 'react'
import {
  DndContext, DragOverlay, MouseSensor, TouchSensor,
  defaultDropAnimationSideEffects, pointerWithin, useSensor, useSensors,
} from '@dnd-kit/core'
import type { CollisionDetection, DragEndEvent, DragOverEvent, DragStartEvent } from '@dnd-kit/core'
import type { Task } from '../../../models/tasks/TaskTypes'
import { kanbanConfig } from '../kanbanConfig'
import { useKanbanColumns } from '../../../hooks/columns/useKanbanColumns'
import { useKanbanProject } from '../../../hooks/projects/useKanbanProject'
import { useKanbanTasks } from '../../../hooks/tasks/useKanbanTasks'
import { useIsMobile } from '../../../hooks/useIsMobile'
import { emptyTaskFilters, filterTasks } from '../../../utils/tasks/filterTasks'
import type { TaskFilters } from '../../../utils/tasks/filterTasks'
import { moveTask as moveTaskInBoard } from '../../../utils/tasks/moveTask'
import { KanbanColumn } from '../column/KanbanColumn'
import { MoveErrorToast } from '../feedback/MoveErrorToast'
import { BoardFilters } from '../filters/BoardFilters'
import { BoardHeader } from '../header/BoardHeader'
import { MobileBoard } from '../mobile/MobileBoard'
import { TaskCardContent } from '../task/TaskCardContent'
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
  const { project, status: projectStatus } = useKanbanProject()
  const { columns, status: columnsStatus } = useKanbanColumns()
  const {
    tasks, status: tasksStatus, moveError, moveTask, retryMove, dismissMoveError,
  } = useKanbanTasks()
  const [filters, setFilters] = useState<TaskFilters>(emptyTaskFilters)
  const isMobile = useIsMobile()
  const [activeTaskId, setActiveTaskId] = useState<number | null>(null)
  const [previewTasks, setPreviewTasks] = useState<Task[] | null>(null)
  const displayedTasks = previewTasks ?? tasks
  const visibleTaskIds = new Set(filterTasks(displayedTasks, filters).map((task) => task.id))
  const activeTask = displayedTasks.find((task) => task.id === activeTaskId)
  const assignees = [...new Set(tasks.map((task) => task.assignee))].sort((first, second) => first.localeCompare(second, 'cs'))
  const isLoading = [projectStatus, columnsStatus, tasksStatus].includes('loading')
  const hasError = [projectStatus, columnsStatus, tasksStatus].includes('error')
  const sensors = useSensors(
    useSensor(MouseSensor, {
      activationConstraint: { distance: kanbanConfig.drag.activationDistance },
    }),
    useSensor(TouchSensor, {
      activationConstraint: { delay: kanbanConfig.drag.touchDelay, tolerance: kanbanConfig.drag.activationDistance },
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

    if (target) void moveTask(target.task.id, target.columnId, target.position)
  }

  if (hasError) {
    return (
      <section className="kanban-board">
        <p role="alert" className="kanban-board-message">
          Nástěnku se nepodařilo načíst. Zkuste obnovit stránku.
        </p>
      </section>
    )
  }

  if (isLoading || !project) {
    return (
      <section className="kanban-board" aria-busy="true">
        <p role="status" className="kanban-board-message">Načítání nástěnky…</p>
      </section>
    )
  }

  return (
    <section className="kanban-board" aria-labelledby="kanban-board-title">
      <BoardHeader project={project} />
      <BoardFilters filters={filters} assignees={assignees} onChange={setFilters} />
      {tasks.length === 0 && (
        <p className="kanban-board-message">Na nástěnce zatím nejsou žádné úkoly.</p>
      )}
      {tasks.length > 0 && visibleTaskIds.size === 0 && (
        <p className="kanban-board-message">Žádný úkol neodpovídá filtrům.</p>
      )}
      {isMobile ? (
        <MobileBoard
          columns={columns}
          tasks={tasks.filter((task) => visibleTaskIds.has(task.id))}
          onMoveTask={(taskId, columnId) => void moveTask(
            taskId, columnId, tasks.filter((task) => task.columnId === columnId).length,
          )}
        />
      ) : (
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
                tasks={displayedTasks
                  .filter((task) => task.columnId === column.id
                    && (visibleTaskIds.has(task.id) || task.id === activeTaskId))
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
                <TaskCardContent
                  task={activeTask}
                  isDone={columns.find((column) => column.id === activeTask.columnId)?.isDone ?? false}
                />
              </article>
            )}
          </DragOverlay>
        </DndContext>
      )}
      {moveError && (
        <MoveErrorToast
          taskTitle={moveError.task.title}
          columnTitle={columns.find((column) => column.id === moveError.task.columnId)?.title ?? ''}
          onRetry={retryMove}
          onDismiss={dismissMoveError}
        />
      )}
    </section>
  )
}
