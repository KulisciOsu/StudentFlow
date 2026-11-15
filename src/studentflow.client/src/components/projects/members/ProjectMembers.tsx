import { memberInitials } from '../../../utils/projects/memberInitials'
import type { ProjectMembersProps } from './ProjectMembersTypes'
import './ProjectMembers.scss'

/// <summary>
/// Sekce „Členové týmu“ podle wireframu nastavení projektu: počet členů, název týmu a řádek pro každého člena.
/// </summary>
export function ProjectMembers({ teamName, members, currentUserId, children }: ProjectMembersProps) {
  return (
    <section className="project-members" aria-labelledby="project-members-title">
      <header className="project-members__header">
        <h3 id="project-members-title" className="project-members__title">
          Členové týmu <span className="project-members__count">{members.length}</span>
        </h3>
        <p className="project-members__team">{teamName}</p>
      </header>

      {members.length === 0
        ? <p className="project-members__empty">V týmu zatím nikdo není.</p>
        : (
          <ul className="project-members__list">
            {members.map(member => (
              <li key={member.userId} className="project-members__row">
                <span className="project-members__avatar" aria-hidden="true">{memberInitials(member.displayName)}</span>
                <span className="project-members__person">
                  <span className="project-members__name">
                    {member.displayName}{member.userId === currentUserId ? ' (vy)' : ''}
                  </span>
                  <span className="project-members__email">{member.email}</span>
                </span>
                <span className={`project-members__badge${member.isLeader ? ' project-members__badge--leader' : ''}`}>
                  {member.isLeader ? 'Vedoucí' : 'Člen'}
                </span>
              </li>
            ))}
          </ul>
        )}
      {children}
    </section>
  )
}
