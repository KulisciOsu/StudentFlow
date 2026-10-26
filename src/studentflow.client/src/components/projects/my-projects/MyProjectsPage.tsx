import { useMyProjects } from '../../../hooks/projects/useMyProjects'
import { MyTasksTable } from './MyTasksTable'
import { ProjectCard } from './ProjectCard'
import type { MyProjectsPageProps } from './MyProjectsPageTypes'
import './MyProjectsPage.scss'

/// <summary>
/// Zobrazuje rozcestník studenta: jeho projekty a otevřené úkoly.
/// </summary>
export function MyProjectsPage({ onCreateProject, onOpenProject, onOpenTask }: MyProjectsPageProps) {
  const { projects, tasks, status } = useMyProjects()

  return (
    <section className="my-projects" aria-labelledby="my-projects-title" aria-busy={status === 'loading'}>
      <header className="my-projects__header">
        <div>
          <h2 id="my-projects-title">Moje projekty</h2>
          <p>Projekty, ve kterých jste členem nebo vedoucím týmu.</p>
        </div>
        <button type="button" className="my-projects__create" onClick={onCreateProject}>
          <span aria-hidden="true">+</span> Založit projekt
        </button>
      </header>

      {status === 'loading' && <p role="status">Načítání projektů…</p>}
      {status === 'error' && (
        <p role="alert">Projekty se nepodařilo načíst. Zkuste obnovit stránku.</p>
      )}

      {status === 'ready' && (
        <>
          <ul className="my-projects__grid">
            {projects.map((project) => (
              <li key={project.id}>
                <ProjectCard project={project} onOpen={onOpenProject} />
              </li>
            ))}
            <li>
              <button type="button" className="my-projects__new" onClick={onCreateProject}>
                <span className="my-projects__new-plus" aria-hidden="true">+</span>
                <strong>Založit nový projekt</strong>
                <span>Stanete se vedoucím jeho týmu.</span>
              </button>
            </li>
          </ul>

          <MyTasksTable tasks={tasks} onOpenTask={onOpenTask} />
        </>
      )}
    </section>
  )
}
