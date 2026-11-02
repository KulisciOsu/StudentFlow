import { useState, type FormEvent } from 'react'
import './CreateProjectPage.scss'

type FormValues = {
  projectName: string
  description: string
  deadline: string
  teamName: string
}

const initialValues: FormValues = {
  projectName: 'Rezervační systém knihovny',
  description: '',
  deadline: '2027-01-15',
  teamName: '',
}

function BellIcon() {
  return (
    <svg viewBox="0 0 24 24" aria-hidden="true">
      <path d="M18 8a6 6 0 0 0-12 0c0 7-3 7-3 9h18c0-2-3-2-3-9M10 21h4" />
    </svg>
  )
}

function CalendarIcon() {
  return (
    <svg viewBox="0 0 24 24" aria-hidden="true">
      <rect x="3.5" y="5" width="17" height="16" rx="2" />
      <path d="M7.5 3v4M16.5 3v4M4 9.5h16" />
    </svg>
  )
}

function InfoIcon() {
  return (
    <svg viewBox="0 0 24 24" aria-hidden="true">
      <circle cx="12" cy="12" r="9" />
      <path d="M12 11v5M12 8h.01" />
    </svg>
  )
}

export function CreateProjectPage() {
  const [values, setValues] = useState<FormValues>(initialValues)
  const [profileOpen, setProfileOpen] = useState(false)
  const [createdProject, setCreatedProject] = useState<string | null>(null)

  function update<K extends keyof FormValues>(key: K, value: FormValues[K]) {
    setValues((current) => ({ ...current, [key]: value }))
    setCreatedProject(null)
  }

  function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setCreatedProject(values.projectName.trim())
  }

  function handleCancel() {
    setValues(initialValues)
    setCreatedProject(null)
  }

  return (
    <main className="sf-shell">
      <header className="sf-topbar">
        <a className="sf-brand" href="/" aria-label="StudentFlow – úvodní stránka">
          <span className="sf-brand-mark">SF</span>
          <span>StudentFlow</span>
        </a>

        <div className="sf-account-area">
          <button className="sf-notification" type="button" aria-label="Oznámení, 3 nepřečtená">
            <BellIcon />
            <span className="sf-notification-count">3</span>
          </button>
          <div className="sf-profile-wrap">
            <button
              className="sf-profile"
              type="button"
              aria-expanded={profileOpen}
              onClick={() => setProfileOpen((open) => !open)}
            >
              <span className="sf-avatar">PS</span>
              <span className="sf-profile-copy">
                <strong>Petra Svobodová</strong>
                <small>Student</small>
              </span>
              <span className={`sf-chevron ${profileOpen ? 'is-open' : ''}`} />
            </button>
            {profileOpen && (
              <div className="sf-profile-menu">
                <strong>Petra Svobodová</strong>
                <span>student@studentflow.cz</span>
                <button type="button" onClick={() => setProfileOpen(false)}>Zavřít nabídku</button>
              </div>
            )}
          </div>
        </div>
      </header>

      <section className="sf-content">
        <div className="sf-content-inner">
          <nav className="sf-breadcrumb" aria-label="Drobečková navigace">
            <a href="#projects">Moje projekty</a>
            <span aria-hidden="true"> / </span>
            <span>Založit projekt</span>
          </nav>

          <div className="sf-page-heading">
            <h1>Založit projekt</h1>
            <p>Projekt se zakládá společně s týmem. Vy se stanete jeho vedoucím.</p>
          </div>

          <form className="sf-form-card" onSubmit={handleSubmit}>
            <section className="sf-form-section" aria-labelledby="project-section-title">
              <h2 id="project-section-title">1 · Projekt</h2>

              <div className="sf-field">
                <label htmlFor="project-name">Název projektu <span>*</span></label>
                <input
                  id="project-name"
                  name="projectName"
                  type="text"
                  value={values.projectName}
                  onChange={(event) => update('projectName', event.target.value)}
                  maxLength={160}
                  required
                />
              </div>

              <div className="sf-field">
                <label htmlFor="project-description">Popis</label>
                <textarea
                  id="project-description"
                  name="description"
                  placeholder="Stručně popište zadání projektu a jeho cíl."
                  value={values.description}
                  onChange={(event) => update('description', event.target.value)}
                  rows={3}
                />
              </div>

              <div className="sf-field sf-date-field">
                <label htmlFor="project-deadline">Plánovaný termín odevzdání <span>*</span></label>
                <div className="sf-date-control">
                  <input
                    id="project-deadline"
                    name="deadline"
                    type="date"
                    value={values.deadline}
                    onChange={(event) => update('deadline', event.target.value)}
                    required
                  />
                  <CalendarIcon />
                </div>
              </div>
            </section>

            <div className="sf-form-divider" />

            <section className="sf-form-section sf-team-section" aria-labelledby="team-section-title">
              <h2 id="team-section-title">2 · Tým</h2>
              <div className="sf-field">
                <label htmlFor="team-name">Název týmu <span>*</span></label>
                <input
                  id="team-name"
                  name="teamName"
                  type="text"
                  placeholder="Např. Tým Sova"
                  value={values.teamName}
                  onChange={(event) => update('teamName', event.target.value)}
                  maxLength={100}
                  required
                />
              </div>

              <div className="sf-info-note">
                <InfoIcon />
                <span>Členy týmu a vyučujícího přidáte po založení v Nastavení projektu.</span>
              </div>
            </section>

            {createdProject && (
              <div className="sf-success-message" role="status">
                Formulář je vyplněný. Projekt „{createdProject}“ je připraven k napojení na API.
              </div>
            )}

            <div className="sf-form-actions">
              <button className="sf-button sf-button-secondary" type="button" onClick={handleCancel}>Zrušit</button>
              <button className="sf-button sf-button-primary" type="submit">Založit projekt</button>
            </div>
          </form>
        </div>
      </section>
    </main>
  )
}
