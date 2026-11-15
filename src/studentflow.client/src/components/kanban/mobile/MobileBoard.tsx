import { useState } from 'react'
import { TaskCardContent } from '../task/TaskCardContent'
import { isOverdue } from '../../../utils/tasks/taskDisplay'
import { MobileBottomNav } from './MobileBottomNav'
import type { MobileBoardProps } from './MobileBoardTypes'
import '../task/TaskCard.scss'
import './MobileBoard.scss'

/// <summary>
/// Zobrazuje nástěnku na mobilu: sloupce v záložkách a přesun tlačítkem.
/// </summary>
export function MobileBoard({ columns, tasks, isReadOnly, onMoveTask }: MobileBoardProps) {
  const [selectedColumnId, setSelectedColumnId] = useState<number | null>(null)
  const activeColumn = columns.find((column) => column.id === selectedColumnId) ?? columns[0]
  const columnTasks = tasks
    .filter((task) => task.columnId === activeColumn?.id)
    .sort((first, second) => first.position - second.position)

  return (
    <div className="mobile-board">
      <div className="mobile-board-tabs" role="tablist" aria-label="Sloupce">
        {columns.map((column) => (
          <button
            key={column.id}
            type="button"
            role="tab"
            aria-selected={column.id === activeColumn?.id}
            className="mobile-board-tab"
            onClick={() => setSelectedColumnId(column.id)}
          >
            <span>{column.title}</span>
            <span className="mobile-board-tab-count">
              {tasks.filter((task) => task.columnId === column.id).length}
            </span>
          </button>
        ))}
      </div>

      <ul className="mobile-board-tasks" role="tabpanel">
        {columnTasks.map((task) => {
          const classNames = ['task-card', 'task-card--static']
          if (activeColumn.isDone) classNames.push('task-card--done')
          else if (isOverdue(task.dueDate)) classNames.push('task-card--overdue')

          return (
            <li key={task.id}>
              <article className={classNames.join(' ')}>
                <TaskCardContent task={task} isDone={activeColumn.isDone} />
                {task.canMove && (
                  <label className="mobile-board-move">
                    <span>Přesunout</span>
                    <select
                      value=""
                      aria-label={`Přesunout úkol ${task.title}`}
                      onChange={(event) => onMoveTask(task.id, Number(event.target.value))}
                    >
                      <option value="" disabled>Přesunout do…</option>
                      {columns
                        .filter((column) => column.id !== task.columnId)
                        .map((column) => (
                          <option key={column.id} value={column.id}>{column.title}</option>
                        ))}
                    </select>
                  </label>
                )}
              </article>
            </li>
          )
        })}
      </ul>
      {columnTasks.length === 0 && <p className="mobile-board-empty">Žádné úkoly</p>}
      <MobileBottomNav showNewTask={!isReadOnly} />
    </div>
  )
}
