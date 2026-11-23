import { useProjectMembers } from '../../../hooks/projects/useProjectMembers'
import { ProjectMembers } from '../members/ProjectMembers'
import type { ProjectSettingsPageProps } from './ProjectSettingsPageTypes'
import './ProjectSettingsPage.scss'

/// <summary>
/// Stránka „Nastavení projektu“ jen pro vedoucího týmu; zatím obsahuje sekci „Členové týmu“.
/// </summary>
/// <remarks>
/// Běžný člen uvidí „Nemáte oprávnění“. Nečlen dostane od API stejnou chybu jako u neexistujícího projektu.
/// Skrytí stránky je jen pohodlí pro uživatele; oprávnění vždy ověřuje server.
/// </remarks>
export function ProjectSettingsPage({ projectId, currentUserId, home, onNavigate }: ProjectSettingsPageProps) {
  const { state, reload } = useProjectMembers(projectId)

  /// <summary>
  /// Vybere obsah podle stavu načtení a role přihlášeného uživatele.
  /// </summary>
  function renderContent() {
    if (state.status === 'loading')
      return <p className="project-settings__status" role="status">Načítáme členy týmu…</p>

    if (state.status === 'error')
      return (
        <div className="project-settings__notice" role="alert">
          <p>{state.message}</p>
          {!state.notFound && <button type="button" onClick={reload}>Zkusit znovu</button>}
        </div>
      )

    const isLeader = state.data.members.some(member => member.userId === currentUserId && member.isLeader)
    if (!isLeader)
      return (
        <div className="project-settings__notice" role="alert">
          <p className="project-settings__notice-title">Nemáte oprávnění</p>
          <p>Nastavení projektu může měnit jen vedoucí týmu.</p>
        </div>
      )

    return (
      <ProjectMembers
        projectId={projectId}
        onChanged={reload}
        teamName={state.data.teamName}
        members={state.data.members}
        currentUserId={currentUserId}
      />
    )
  }

  return (
    <section className="project-settings" aria-labelledby="project-settings-title">
      <nav className="project-settings__breadcrumb" aria-label="Drobečková navigace">
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
        <a
          href={`/board/${projectId}`}
          onClick={(event) => {
            event.preventDefault()
            onNavigate(`/board/${projectId}`)
          }}
        >
          Nástěnka
        </a>
        <span aria-hidden="true"> / </span>
        <span>Nastavení projektu</span>
      </nav>
      <h2 id="project-settings-title" className="project-settings__title">Nastavení projektu</h2>
      {renderContent()}
    </section>
  )
}
