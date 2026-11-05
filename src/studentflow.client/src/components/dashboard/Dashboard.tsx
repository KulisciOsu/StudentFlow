
import { useState } from 'react'
import type { Project, Task, DashboardProps, ProjectCardProps } from './DashboardPageTypes'
import './Dashboard.scss'

/** Ukázková data projektů pro přehled na dashboardu. */
const projects: Project[] = [
  {
    id: 1,
    name: 'Rezervační systém knihovny',
    team: 'Tým Sova',
    membersCount: 5,
    role: 'Vedoucí',
    completed: 3,
    total: 10,
    openTasks: 1,
    overdueTasks: 0,
    milestone: 'Inception, 31. 10.',
    members: ['PS', 'JN', 'TD', 'LČ', 'MK'],
  },
  {
    id: 2,
    name: 'Analýza dat: semestrální práce',
    team: 'Tým Delta',
    membersCount: 4,
    role: 'Člen',
    completed: 5,
    total: 8,
    openTasks: 2,
    overdueTasks: 1,
    milestone: 'Odevzdání, 12. 12.',
    members: ['KB', 'PS', 'OV', 'RH'],
  },
]

/** Ukázková data úkolů pro přehled napříč projekty. */
const tasks: Task[] = [
  {
    id: 1,
    name: 'Čištění datasetu',
    project: 'Analýza dat: semestrální práce',
    deadline: '24. 10.',
    priority: 'Vysoká',
    status: 'Rozpracováno',
    overdue: true,
  },
  {
    id: 2,
    name: 'Wireframy obrazovek',
    project: 'Rezervační systém knihovny',
    deadline: '30. 10.',
    priority: 'Střední',
    status: 'Rozpracováno',
  },
  {
    id: 3,
    name: 'Regresní model',
    project: 'Analýza dat: semestrální práce',
    deadline: '6. 11.',
    priority: 'Střední',
    status: 'K řešení',
  },
  {
    id: 4,
    name: 'Analýza požadavků',
    project: 'Rezervační systém knihovny',
    deadline: '20. 10.',
    priority: 'Střední',
    status: 'Hotovo',
    completed: true,
  },
]

/** Vykreslí ikonu oznámení v záhlaví dashboardu. */
function BellIcon() {
  return (
    <svg
      width="21"
      height="21"
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth="1.8"
      strokeLinecap="round"
      strokeLinejoin="round"
      aria-hidden="true"
    >
      <path d="M18 8a6 6 0 0 0-12 0c0 7-3 7-3 9h18c0-2-3-2-3-9" />
      <path d="M10 21h4" />
    </svg>
  )
}

/**
 * Zobrazí kartu projektu s průběhem plnění, termínem milníku a členy týmu.
 * @param project Projekt zobrazovaný na kartě.
 */
function ProjectCard({ project }: ProjectCardProps) {
  const progress = (project.completed / project.total) * 100

  return (
    <article className="dashboard__project-card">
      <div className="dashboard__project-heading">
        <div>
          <h3>{project.name}</h3>
          <p>
            {project.team} · {project.membersCount} členů
          </p>
        </div>

        <span
          className={
            project.role === 'Vedoucí'
              ? 'dashboard__role dashboard__role--leader'
              : 'dashboard__role'
          }
        >
          {project.role}
        </span>
      </div>

      <div className="dashboard__project-progress">
        <p>
          {project.completed} z {project.total} úkolů hotovo
        </p>

        <div
          className="dashboard__progress"
          role="progressbar"
          aria-label={`Dokončení projektu ${project.name}`}
          aria-valuenow={project.completed}
          aria-valuemin={0}
          aria-valuemax={project.total}
        >
          <div style={{ width: `${progress}%` }} />
        </div>
      </div>

      <div className="dashboard__project-info">
        <p>
          <strong>Moje úkoly:</strong> {project.openTasks} otevřené
          {project.overdueTasks > 0 && (
            <strong className="dashboard__overdue">
              {' '}· {project.overdueTasks} po termínu
            </strong>
          )}
        </p>
        <p>
          Nejbližší milník: {project.milestone}
        </p>
      </div>

      <div
        className="dashboard__members"
        aria-label={`Členové týmu ${project.team}`}
      >
        {project.members.map((initials, index) => (
          <span key={`${initials}-${index}`} title={initials}>
            {initials}
          </span>
        ))}
      </div>
    </article>
  )
}

/**
 * Hlavní přehled projektů a úkolů uživatele.
 * @param onCreateProject Callback pro přechod na formulář založení projektu.
 */
export function Dashboard({ onCreateProject, embedded = false }: DashboardProps) {
  const [activeTab, setActiveTab] = useState<'open' | 'done'>('open')
  const [profileOpen, setProfileOpen] = useState(false)
  const [notice, setNotice] = useState('')
  const Container = embedded ? 'section' : 'main'
  const ProjectsHeading = embedded ? 'h2' : 'h1'

  const visibleTasks = tasks.filter((task) =>
    activeTab === 'done' ? task.completed : !task.completed
  )

  const openTaskCount = tasks.filter((task) => !task.completed).length

  /**
   * Zobrazí informační hlášku pro dosud nedostupnou funkci a zavře nabídku profilu.
   * @param message Text hlášky.
   */
  function showPlaceholder(message: string) {
    setNotice(message)
    setProfileOpen(false)
  }

  return (
    <div className={`dashboard${embedded ? ' dashboard--embedded' : ''}`}>
      {!embedded && <header className="dashboard__header">
        <a
          className="dashboard__brand"
          href="/dashboard"
          aria-label="StudentFlow – úvodní dashboard"
        >
          <span className="dashboard__logo">SF</span>
          <strong>StudentFlow</strong>
        </a>

        <div className="dashboard__header-actions">
          <button
            className="dashboard__notifications"
            type="button"
            aria-label="Oznámení"
            onClick={() => showPlaceholder('Oznámení budou doplněna později.')}
          >
            <BellIcon />
            <span className="dashboard__notification-count">3</span>
          </button>

          <div className="dashboard__profile-wrapper">
            <button
              className="dashboard__account"
              type="button"
              aria-expanded={profileOpen}
              aria-label="Uživatelský profil"
              onClick={() => {
                setProfileOpen((previous) => !previous)
                setNotice('')
              }}
            >
              <span className="dashboard__avatar">PS</span>
              <span className="dashboard__account-info">
                <strong>Petra Svobodová</strong>
                <small>Student</small>
              </span>
              <span aria-hidden="true">⌄</span>
            </button>

            {profileOpen && (
              <div className="dashboard__profile-menu">
                <button
                  type="button"
                  onClick={() => showPlaceholder('Nastavení profilu zatím není dostupné.')}
                >
                  Nastavení
                </button>
                <button
                  type="button"
                  onClick={() => showPlaceholder('Odhlášení bude doplněno později.')}
                >
                  Odhlásit se
                </button>
              </div>
            )}
          </div>
        </div>
      </header>}

      <Container className="dashboard__content">
        {notice && (
          <div className="dashboard__notice" role="status">
            <span>{notice}</span>
            <button
              type="button"
              aria-label="Zavřít upozornění"
              onClick={() => setNotice('')}
            >
              ×
            </button>
          </div>
        )}

        <section aria-labelledby="projects-title">
          <div className="dashboard__section-heading">
            <div>
              <ProjectsHeading id="projects-title">Moje projekty</ProjectsHeading>
              <p>Projekty, ve kterých jste členem nebo vedoucím týmu.</p>
            </div>

            <button
              type="button"
              className="dashboard__create-button"
              onClick={onCreateProject}
            >
              <span aria-hidden="true">+ </span>Založit projekt
            </button>
          </div>

          <div className="dashboard__projects">
            {projects.map((project) => (
              <ProjectCard key={project.id} project={project} />
            ))}

            <button
              type="button"
              className="dashboard__new-project"
              onClick={onCreateProject}
            >
              <span className="dashboard__new-project-plus">+</span>
              <strong>Založit nový projekt</strong>
              <small>Stanete se vedoucím jeho týmu.</small>
            </button>
          </div>
        </section>

        <section
          className="dashboard__tasks-section"
          aria-labelledby="tasks-title"
        >
          <div className="dashboard__tasks-heading">
            <h2 id="tasks-title">Moje úkoly napříč projekty</h2>

            <div className="dashboard__tabs" role="group" aria-label="Filtr úkolů">
              <button
                type="button"
                className={activeTab === 'open' ? 'active' : ''}
                aria-pressed={activeTab === 'open'}
                onClick={() => setActiveTab('open')}
              >
                Otevřené ({openTaskCount})
              </button>
              <button
                type="button"
                className={activeTab === 'done' ? 'active' : ''}
                aria-pressed={activeTab === 'done'}
                onClick={() => setActiveTab('done')}
              >
                Hotové
              </button>
            </div>
          </div>

          <div className="dashboard__table-wrapper">
            <table className="dashboard__table">
              <thead>
                <tr>
                  <th>Úkol</th>
                  <th>Projekt</th>
                  <th>Termín</th>
                  <th>Priorita</th>
                  <th>Stav</th>
                </tr>
              </thead>
              <tbody>
                {visibleTasks.map((task) => (
                  <tr key={task.id}>
                    <td>
                      <button
                        type="button"
                        className="dashboard__task-link"
                        onClick={() => showPlaceholder(`Detail úkolu „${task.name}“ bude doplněn později.`)}
                      >
                        {task.name}
                      </button>
                    </td>
                    <td>{task.project}</td>
                    <td className={task.overdue ? 'dashboard__overdue' : ''}>
                      {task.deadline}
                      {task.overdue && ' · po termínu'}
                    </td>
                    <td>
                      <span
                        className={
                          task.priority === 'Vysoká'
                            ? 'dashboard__priority dashboard__priority--high'
                            : 'dashboard__priority'
                        }
                      >
                        {task.priority}
                      </span>
                    </td>
                    <td>{task.status}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </section>
      </Container>
    </div>
  )
}
