import { formatDate } from '../../../utils/tasks/taskDisplay'
import type { BoardHeaderProps } from './BoardHeaderTypes'
import './BoardHeader.scss'

/// <summary>
/// Zobrazuje název projektu a termín odevzdání.
/// </summary>
export function BoardHeader({ project }: BoardHeaderProps) {
  return (
    <header className="board-header">
      <p className="board-header-project">{project.name} · {project.teamName}</p>
      <h2 id="kanban-board-title">Nástěnka</h2>
      {project.dueDate && (
        <p className="board-header-due">Odevzdání {formatDate(project.dueDate)}</p>
      )}
    </header>
  )
}
