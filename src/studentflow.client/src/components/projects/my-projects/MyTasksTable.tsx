import { useState } from 'react'
import type { TaskPriority, TaskStatus } from '../../../models/projects/MyProjectsTypes'
import { formatShortDate, isOverdue } from '../../../utils/projects/formatting'
import type { MyTasksFilter, MyTasksTableProps } from './MyTasksTableTypes'
import './MyTasksTable.scss'

const priorityLabels: Record<TaskPriority, string> = {
  high: 'Vysoká',
  medium: 'Střední',
  low: 'Nízká',
}

const statusLabels: Record<TaskStatus, string> = {
  todo: 'K řešení',
  inProgress: 'Rozpracováno',
  done: 'Hotovo',
}

const filters: { value: MyTasksFilter, label: string }[] = [
  { value: 'open', label: 'Otevřené' },
  { value: 'done', label: 'Hotové' },
]

/// <summary>
/// Zobrazuje úkoly studenta napříč projekty s přepínačem otevřené a hotové.
/// </summary>
export function MyTasksTable({ tasks, onOpenTask }: MyTasksTableProps) {
  const [filter, setFilter] = useState<MyTasksFilter>('open')
  const openCount = tasks.filter((task) => task.status !== 'done').length
  const visibleTasks = tasks.filter((task) => (filter === 'done') === (task.status === 'done'))

  return (
    <section className="my-tasks" aria-labelledby="my-tasks-title">
      <div className="my-tasks__header">
        <h2 id="my-tasks-title">Moje úkoly napříč projekty</h2>
        <div className="my-tasks__tabs" role="tablist" aria-label="Filtr úkolů">
          {filters.map(({ value, label }) => (
            <button
              key={value}
              type="button"
              role="tab"
              id={`my-tasks-tab-${value}`}
              aria-selected={filter === value}
              aria-controls="my-tasks-panel"
              className={`my-tasks__tab${filter === value ? ' my-tasks__tab--active' : ''}`}
              onClick={() => setFilter(value)}
            >
              {label}{value === 'open' ? ` (${openCount})` : ''}
            </button>
          ))}
        </div>
      </div>

      <div id="my-tasks-panel" role="tabpanel" aria-labelledby={`my-tasks-tab-${filter}`}>
        {visibleTasks.length === 0 ? (
          <p className="my-tasks__empty">
            {filter === 'open' ? 'Nemáte žádné otevřené úkoly.' : 'Zatím nemáte žádné hotové úkoly.'}
          </p>
        ) : (
          <div className="my-tasks__table-wrap">
            <table className="my-tasks__table">
              <thead>
                <tr>
                  <th scope="col">Úkol</th>
                  <th scope="col">Projekt</th>
                  <th scope="col">Termín</th>
                  <th scope="col">Priorita</th>
                  <th scope="col">Stav</th>
                </tr>
              </thead>
              <tbody>
                {visibleTasks.map((task) => {
                  const overdue = isOverdue(task)

                  return (
                    <tr key={task.id}>
                      <td>
                        <button type="button" className="my-tasks__link" onClick={() => onOpenTask(task)}>
                          {task.title}
                        </button>
                      </td>
                      <td className="my-tasks__project">{task.projectName}</td>
                      <td className={overdue ? 'my-tasks__deadline--overdue' : undefined}>
                        {task.deadline ? formatShortDate(task.deadline) : '–'}
                        {overdue && ' · po termínu'}
                      </td>
                      <td>
                        <span className={`my-tasks__priority my-tasks__priority--${task.priority}`}>
                          {priorityLabels[task.priority]}
                        </span>
                      </td>
                      <td>{statusLabels[task.status]}</td>
                    </tr>
                  )
                })}
              </tbody>
            </table>
          </div>
        )}
      </div>
    </section>
  )
}
