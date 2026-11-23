
import { useEffect, useState } from 'react'
import type {
  Project,
  Task,
  DashboardProps,
  ProjectCardProps,
} from './DashboardPageTypes'
import { getDashboard } from '../../services/dashboard/dashboardApi'
import { formatMemberCount } from '../../utils/projects/memberCount'
import { memberInitials } from '../../utils/projects/memberInitials'
import './Dashboard.scss'

/**
 * Zobrazí kartu skutečného projektu z databáze.
 */
function ProjectCard({ project, onOpen }: ProjectCardProps) {
  const maximum = Math.max(project.total, 1)
  const completed = Math.max(0, Math.min(project.completed, maximum))
  const progress = project.total > 0
    ? Math.min(100, Math.max(0, (completed / project.total) * 100))
    : 0

  return (
    <button
      type="button"
      className="dashboard__project-card"
      style={{
        width: '100%',
        textAlign: 'left',
        font: 'inherit',
        color: 'inherit',
        cursor: 'pointer',
      }}
      onClick={() => onOpen(project.id)}
      aria-label={`Otevřít projekt ${project.name}`}
    >
      <div className="dashboard__project-heading">
        <div>
          <h3>{project.name}</h3>
          <p>
            {project.team} · {formatMemberCount(project.membersCount)}
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
          aria-valuenow={completed}
          aria-valuemin={0}
          aria-valuemax={maximum}
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
          Nejbližší milník: {project.milestone || 'Neuveden'}
        </p>
      </div>

      <div
        className="dashboard__members"
        aria-label={`Členové týmu ${project.team}`}
      >
        {project.members.map((member, index) => (
          <span key={`${member}-${index}`} title={member}>
            {memberInitials(member)}
          </span>
        ))}
      </div>
    </button>
  )
}

/**
 * Hlavní přehled skutečných projektů a úkolů uživatele.
 */
export function Dashboard({
  onCreateProject,
  onOpenProject,
}: DashboardProps) {
  const [activeTab, setActiveTab] = useState<'open' | 'done'>('open')
  const [projects, setProjects] = useState<Project[]>([])
  const [tasks, setTasks] = useState<Task[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [reloadKey, setReloadKey] = useState(0)

  useEffect(() => {
    const controller = new AbortController()

    async function loadDashboard() {
      setLoading(true)
      setError('')

      try {
        const data = await getDashboard(controller.signal)

        setProjects(data.projects)
        setTasks(data.tasks)
      } catch (loadError) {
        if (controller.signal.aborted) return

        setError(
          loadError instanceof Error
            ? loadError.message
            : 'Nepodařilo se načíst dashboard.',
        )
      } finally {
        if (!controller.signal.aborted) {
          setLoading(false)
        }
      }
    }

    void loadDashboard()

    return () => controller.abort()
  }, [reloadKey])

  const visibleTasks = tasks.filter((task) =>
    activeTab === 'done' ? task.completed : !task.completed,
  )

  const openTaskCount = tasks.filter((task) => !task.completed).length
  const doneTaskCount = tasks.filter((task) => task.completed).length

  return (
    <div className="dashboard">
      <section className="dashboard__content">

        <section aria-labelledby="projects-title">
          <div className="dashboard__section-heading">
            <div>
              <h2 id="projects-title">
                Moje projekty
              </h2>
              <p>
                Projekty, ve kterých jste členem nebo vedoucím týmu.
              </p>
            </div>

            <button
              type="button"
              className="dashboard__create-button"
              onClick={onCreateProject}
            >
              <span aria-hidden="true">+ </span>Založit projekt
            </button>
          </div>

          {error && (
            <div className="dashboard__notice" role="alert">
              <span>{error}</span>
              <button
                type="button"
                onClick={() => setReloadKey((value) => value + 1)}
              >
                Zkusit znovu
              </button>
            </div>
          )}

          {loading ? (
            <p role="status">Načítáme vaše projekty…</p>
          ) : !error && projects.length === 0 ? (
            <p role="status">
              Zatím nemáte žádný projekt. Založte svůj první projekt.
            </p>
          ) : (
            <div className="dashboard__projects">
              {projects.map((project) => (
                <ProjectCard
                  key={project.id}
                  project={project}
                  onOpen={onOpenProject}
                />
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
          )}
        </section>

        <section
          className="dashboard__tasks-section"
          aria-labelledby="tasks-title"
        >
          <div className="dashboard__tasks-heading">
            <h2 id="tasks-title">Moje úkoly napříč projekty</h2>

            <div
              className="dashboard__tabs"
              role="group"
              aria-label="Filtr úkolů"
            >
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
                Hotové ({doneTaskCount})
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
                {loading ? (
                  <tr>
                    <td colSpan={5}>Načítáme úkoly…</td>
                  </tr>
                ) : visibleTasks.length === 0 ? (
                  <tr>
                    <td colSpan={5}>
                      {activeTab === 'done'
                        ? 'Zatím nemáte žádné dokončené úkoly.'
                        : 'Nemáte žádné otevřené úkoly.'}
                    </td>
                  </tr>
                ) : (
                  visibleTasks.map((task) => (
                    <tr key={task.id}>
                      <td>
                        <button
                          type="button"
                          className="dashboard__task-link"
                          title={`Otevřít nástěnku projektu ${task.project}`}
                          onClick={() => onOpenProject(task.projectId)}
                        >
                          {task.name}
                        </button>
                      </td>
                      <td>{task.project}</td>
                      <td
                        className={
                          task.overdue ? 'dashboard__overdue' : ''
                        }
                      >
                        {task.deadline || '—'}
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
                  ))
                )}
              </tbody>
            </table>
          </div>
        </section>
      </section>
    </div>
  )
}
