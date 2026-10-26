import { pluralCs, formatShortDate } from '../../../utils/projects/formatting'
import type { ProjectCardProps } from './ProjectCardTypes'
import './ProjectCard.scss'

/// <summary>
/// Zobrazuje kartu projektu na rozcestníku studenta.
/// </summary>
export function ProjectCard({ project, onOpen }: ProjectCardProps) {
  const { tasksDone, tasksTotal, myOpenTasks, myOverdueTasks, nextMilestone } = project
  const progress = tasksTotal > 0 ? Math.round((tasksDone / tasksTotal) * 100) : 0
  const memberCount = project.members.length
  const isLeader = project.role === 'leader'

  return (
    <article className="project-card" aria-labelledby={`project-${project.id}-title`}>
      <header className="project-card__header">
        <h3 id={`project-${project.id}-title`}>
          <button type="button" className="project-card__open" onClick={() => onOpen(project.id)}>
            {project.name}
          </button>
        </h3>
        <span className={`project-card__role${isLeader ? ' project-card__role--leader' : ''}`}>
          {isLeader ? 'Vedoucí' : 'Člen'}
        </span>
      </header>

      <p className="project-card__team">
        {project.teamName} · {memberCount} {pluralCs(memberCount, ['člen', 'členové', 'členů'])}
      </p>

      <p className="project-card__progress-label">
        {tasksDone} z {tasksTotal} úkolů hotovo
      </p>
      <div
        className="project-card__progress"
        role="progressbar"
        aria-label="Hotové úkoly"
        aria-valuemin={0}
        aria-valuemax={tasksTotal}
        aria-valuenow={tasksDone}
      >
        <div className="project-card__progress-bar" style={{ width: `${progress}%` }} />
      </div>

      <p className="project-card__mine">
        Moje úkoly: {myOpenTasks} {pluralCs(myOpenTasks, ['otevřený', 'otevřené', 'otevřených'])}
        {myOverdueTasks > 0 && (
          <strong className="project-card__overdue"> · {myOverdueTasks} po termínu</strong>
        )}
      </p>
      <p className="project-card__milestone">
        Nejbližší milník: {nextMilestone
          ? `${nextMilestone.name}, ${formatShortDate(nextMilestone.date)}`
          : 'žádný'}
      </p>

      <ul className="project-card__members" aria-label="Členové týmu">
        {project.members.map((member) => (
          <li key={member.id} title={member.name}>
            <span aria-hidden="true">{member.initials}</span>
            <span className="visually-hidden">{member.name}</span>
          </li>
        ))}
      </ul>
    </article>
  )
}
