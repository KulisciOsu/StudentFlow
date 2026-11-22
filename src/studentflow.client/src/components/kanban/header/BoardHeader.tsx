import { formatDate } from '../../../utils/tasks/taskDisplay'
import type { BoardHeaderProps } from './BoardHeaderTypes'
import './BoardHeader.scss'

/// <summary>
/// Zobrazuje drobečkovou navigaci, název projektu, termín odevzdání a vedoucímu odkaz na nastavení.
/// </summary>
export function BoardHeader({ project, home, onNavigate }: BoardHeaderProps) {
  const settingsPath = `/projects/${project.id}/settings`

  return (
    <header className="board-header">
      <nav className="board-header-breadcrumb" aria-label="Drobečková navigace">
        <a
          href={home.path}
          onClick={(event) => {
            event.preventDefault()
            onNavigate(home.path)
          }}
        >
          {home.label}
        </a>
        <span aria-hidden="true"> / </span>
        <span>{project.name} · {project.teamName}</span>
      </nav>
      <div className="board-header-title">
        <h2 id="kanban-board-title">Nástěnka</h2>
        {project.isLeader && (
          <a
            className="board-header-settings"
            href={settingsPath}
            onClick={(event) => {
              event.preventDefault()
              onNavigate(settingsPath)
            }}
          >
            Nastavení projektu
          </a>
        )}
      </div>
      {project.isReadOnly && (
        <p className="board-header-readonly">Jen pro čtení</p>
      )}
      {project.dueDate && (
        <p className="board-header-due">Odevzdání {formatDate(project.dueDate)}</p>
      )}
    </header>
  )
}
