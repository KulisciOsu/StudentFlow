import { CreateProjectForm } from './CreateProjectForm'
import type { CreateProjectPageProps } from './CreateProjectPageTypes'
import './CreateProjectPage.scss'

/// <summary>
/// Zobrazuje obrazovku Založit projekt (UC04).
/// </summary>
export function CreateProjectPage({ onCreated, onCancel }: CreateProjectPageProps) {
  return (
    <section className="create-project-page" aria-labelledby="create-project-title">
      <nav aria-label="Drobečková navigace" className="create-project-page__breadcrumb">
        <button type="button" onClick={onCancel}>Moje projekty</button>
        <span aria-hidden="true"> / </span>
        <span aria-current="page">Založit projekt</span>
      </nav>
      <h2 id="create-project-title">Založit projekt</h2>
      <p className="create-project-page__lead">
        Projekt se zakládá společně s týmem. Vy se stanete jeho vedoucím.
      </p>
      <CreateProjectForm onCreated={onCreated} onCancel={onCancel} />
    </section>
  )
}
