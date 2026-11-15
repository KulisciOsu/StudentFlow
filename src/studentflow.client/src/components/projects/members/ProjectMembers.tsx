import { useMemberRemoval } from '../../../hooks/projects/useMemberRemoval'
import { memberInitials } from '../../../utils/projects/memberInitials'
import type { ProjectMembersProps } from './ProjectMembersTypes'
import './ProjectMembers.scss'

/// <summary>
/// Sekce „Členové týmu“ podle wireframu nastavení projektu: počet členů, název týmu a řádek pro každého člena.
/// </summary>
/// <remarks>
/// U každého člena kromě vedoucího je tlačítko „Odebrat“ s potvrzením přímo v řádku.
/// Zprávy o výsledku jsou pod seznamem, protože řádek odebraného člena po znovunačtení zmizí.
/// </remarks>
export function ProjectMembers({ projectId, onChanged, teamName, members, currentUserId, children }: ProjectMembersProps) {
  const removal = useMemberRemoval(projectId, onChanged)

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
            {members.map(member => {
              const nameId = `project-member-${member.userId}-name`
              const questionId = `project-member-${member.userId}-question`
              return (
                <li key={member.userId} className="project-members__row">
                  <span className="project-members__avatar" aria-hidden="true">{memberInitials(member.displayName)}</span>
                  <span className="project-members__person">
                    <span id={nameId} className="project-members__name">
                      {member.displayName}{member.userId === currentUserId ? ' (vy)' : ''}
                    </span>
                    <span className="project-members__email">{member.email}</span>
                  </span>
                  <span className={`project-members__badge${member.isLeader ? ' project-members__badge--leader' : ''}`}>
                    {member.isLeader ? 'Vedoucí' : 'Člen'}
                  </span>
                  {!member.isLeader && (removal.confirmingUserId === member.userId
                    ? (
                      <span className="project-members__confirm" role="group" aria-labelledby={`${questionId} ${nameId}`}>
                        <span id={questionId} className="project-members__question">Opravdu odebrat?</span>
                        <button
                          className="project-members__button project-members__button--danger"
                          type="button"
                          onClick={() => void removal.confirm(member)}
                          disabled={removal.busy}
                        >
                          {removal.busy ? 'Odebíráme…' : 'Odebrat'}
                        </button>
                        {/* Fokus skočí na bezpečnou volbu, aby Enter z klávesnice nikoho omylem neodebral. */}
                        <button className="project-members__button" type="button" onClick={removal.cancel} disabled={removal.busy} autoFocus>
                          Zrušit
                        </button>
                      </span>
                    )
                    : (
                      <button
                        className="project-members__button"
                        type="button"
                        onClick={() => removal.ask(member.userId)}
                        disabled={removal.busy}
                        aria-describedby={nameId}
                      >
                        Odebrat
                      </button>
                    ))}
                </li>
              )
            })}
          </ul>
        )}
      {removal.error && <p className="project-members__error" role="alert">{removal.error}</p>}
      {removal.success && <p className="project-members__success" role="status">{removal.success}</p>}
      {children}
    </section>
  )
}
