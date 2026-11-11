import { useState, type FormEvent } from 'react'
import { authPost } from '../../../auth/client'
import type {
  CreateProjectPageProps,
  FormErrors,
  FormValues,
} from './CreateProjectPageTypes'
import './CreateProjectPage.scss'

const initialValues: FormValues = {
  projectName: 'Rezervační systém knihovny',
  description: '',
  deadline: '2027-01-15',
  teamName: '',
}

/**
 * Ověří, že odpověď API obsahuje identifikátor vytvořeného projektu.
 */
function readProjectId(value: unknown): number | null {
  if (
    typeof value !== 'object'
    || value === null
    || !('projectId' in value)
    || typeof value.projectId !== 'number'
    || !Number.isInteger(value.projectId)
    || value.projectId <= 0
  ) {
    return null
  }

  return value.projectId
}

/**
 * Získá srozumitelnou chybovou zprávu z odpovědi API.
 */
async function readApiError(response: Response): Promise<string> {
  const value: unknown = await response.json().catch(() => null)

  if (typeof value === 'object' && value !== null) {
    if ('detail' in value && typeof value.detail === 'string') {
      return value.detail
    }

    if ('title' in value && typeof value.title === 'string') {
      return value.title
    }

    if ('errors' in value && typeof value.errors === 'object' && value.errors !== null) {
      const errors = Object.values(value.errors)
      const firstError = errors.find(
        (messages) => Array.isArray(messages) && typeof messages[0] === 'string',
      )

      if (Array.isArray(firstError)) {
        return firstError[0]
      }
    }
  }

  return response.status === 401
    ? 'Přihlášení vypršelo. Přihlaste se znovu.'
    : 'Projekt se nepodařilo vytvořit. Zkuste to znovu.'
}

/**
 * Zobrazuje formulář a vytváří nový projekt prostřednictvím API.
 */
export function CreateProjectPage({
  onBack,
  onCreated,
  embedded = false,
}: CreateProjectPageProps) {
  const [values, setValues] = useState<FormValues>(initialValues)
  const [profileOpen, setProfileOpen] = useState(false)
  const [errors, setErrors] = useState<FormErrors>({})
  const [serverError, setServerError] = useState<string | null>(null)
  const [isSubmitting, setIsSubmitting] = useState(false)

  const Container = embedded ? 'section' : 'main'
  const PageHeading = embedded ? 'h2' : 'h1'
  const SectionHeading = embedded ? 'h3' : 'h2'

  function validateForm(formValues: FormValues): FormErrors {
    const validationErrors: FormErrors = {}

    if (!formValues.projectName.trim()) {
      validationErrors.projectName = 'Zadejte název projektu.'
    } else if (formValues.projectName.trim().length > 160) {
      validationErrors.projectName = 'Název projektu může mít maximálně 160 znaků.'
    }

    if (formValues.description.length > 1000) {
      validationErrors.description = 'Popis může mít maximálně 1 000 znaků.'
    }

    if (!formValues.deadline) {
      validationErrors.deadline = 'Vyberte termín odevzdání.'
    } else {
      const today = new Date()
      const todayString = [
        today.getFullYear(),
        String(today.getMonth() + 1).padStart(2, '0'),
        String(today.getDate()).padStart(2, '0'),
      ].join('-')

      if (formValues.deadline < todayString) {
        validationErrors.deadline = 'Termín odevzdání nemůže být v minulosti.'
      }
    }

    if (!formValues.teamName.trim()) {
      validationErrors.teamName = 'Zadejte název týmu.'
    } else if (formValues.teamName.trim().length > 50) {
      validationErrors.teamName = 'Název týmu může mít maximálně 50 znaků.'
    }

    return validationErrors
  }

  function update<K extends keyof FormValues>(key: K, value: FormValues[K]) {
    const updatedValues = { ...values, [key]: value }

    setValues(updatedValues)
    setServerError(null)

    setErrors((currentErrors) => {
      if (!currentErrors[key]) {
        return currentErrors
      }

      const updatedErrors = validateForm(updatedValues)

      return {
        ...currentErrors,
        [key]: updatedErrors[key],
      }
    })
  }

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()

    if (isSubmitting) return

    const validationErrors = validateForm(values)
    setErrors(validationErrors)
    setServerError(null)

    if (Object.keys(validationErrors).length > 0) return

    setIsSubmitting(true)

    try {
      const response = await authPost('/api/projects', {
        name: values.projectName.trim(),
        description: values.description.trim(),
        dueDate: values.deadline,
        teamName: values.teamName.trim(),
      })

      if (!response.ok) {
        throw new Error(await readApiError(response))
      }

      const responseBody: unknown = await response.json()
      const projectId = readProjectId(responseBody)

      if (projectId === null) {
        throw new Error('Projekt byl vytvořen, ale API nevrátilo platné ID projektu.')
      }

      onCreated(projectId)
    } catch (error) {
      setServerError(
        error instanceof Error
          ? error.message
          : 'Projekt se nepodařilo vytvořit. Zkuste to znovu.',
      )
    } finally {
      setIsSubmitting(false)
    }
  }

  function handleCancel() {
    setValues(initialValues)
    setErrors({})
    setServerError(null)
    onBack()
  }

  return (
    <Container
      className={`sf-shell${embedded ? ' sf-shell--embedded' : ''}`}
      aria-labelledby="create-project-title"
    >
      {!embedded && (
        <header className="sf-topbar">
          <a className="sf-brand" href="/" aria-label="StudentFlow – úvodní stránka">
            <span className="sf-brand-mark">SF</span>
            <span>StudentFlow</span>
          </a>

          <div className="sf-account-area">
            <button
              className="sf-notification"
              type="button"
              aria-label="Oznámení, 3 nepřečtená"
            >
              <svg viewBox="0 0 24 24" aria-hidden="true">
                <path d="M18 8a6 6 0 0 0-12 0c0 7-3 7-3 9h18c0-2-3-2-3-9M10 21h4" />
              </svg>
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
                  <button type="button" onClick={() => setProfileOpen(false)}>
                    Zavřít nabídku
                  </button>
                </div>
              )}
            </div>
          </div>
        </header>
      )}

      <section className="sf-content">
        <div className="sf-content-inner">
          <nav className="sf-breadcrumb" aria-label="Drobečková navigace">
            <a
              href="#projects"
              onClick={(event) => {
                event.preventDefault()
                onBack()
              }}
            >
              Moje projekty
            </a>
            <span aria-hidden="true"> / </span>
            <span>Založit projekt</span>
          </nav>

          <div className="sf-page-heading">
            <PageHeading id="create-project-title">Založit projekt</PageHeading>
            <p>
              Projekt se zakládá společně s týmem. Vy se stanete jeho vedoucím.
            </p>
          </div>

          <form className="sf-form-card" onSubmit={handleSubmit} noValidate>
            <section className="sf-form-section" aria-labelledby="project-section-title">
              <SectionHeading id="project-section-title">1 · Projekt</SectionHeading>

              <div className="sf-field">
                <label htmlFor="project-name">
                  Název projektu <span>*</span>
                </label>
                <input
                  id="project-name"
                  name="projectName"
                  type="text"
                  value={values.projectName}
                  onChange={(event) => update('projectName', event.target.value)}
                  maxLength={160}
                  required
                  className={errors.projectName ? 'sf-input-invalid' : undefined}
                  aria-invalid={Boolean(errors.projectName)}
                  aria-describedby={errors.projectName ? 'project-name-error' : undefined}
                />
                {errors.projectName && (
                  <span id="project-name-error" className="sf-error-message" role="alert">
                    {errors.projectName}
                  </span>
                )}
              </div>

              <div className="sf-field">
                <label htmlFor="project-description">Popis</label>
                <textarea
                  id="project-description"
                  name="description"
                  placeholder="Stručně popište zadání projektu a jeho cíl."
                  value={values.description}
                  onChange={(event) => update('description', event.target.value)}
                  maxLength={1000}
                  rows={3}
                  className={errors.description ? 'sf-input-invalid' : undefined}
                  aria-invalid={Boolean(errors.description)}
                  aria-describedby={
                    errors.description ? 'project-description-error' : undefined
                  }
                />
                {errors.description && (
                  <span id="project-description-error" className="sf-error-message" role="alert">
                    {errors.description}
                  </span>
                )}
              </div>

              <div className="sf-field sf-date-field">
                <label htmlFor="project-deadline">
                  Plánovaný termín odevzdání <span>*</span>
                </label>
                <div className="sf-date-control">
                  <input
                    id="project-deadline"
                    name="deadline"
                    type="date"
                    value={values.deadline}
                    onChange={(event) => update('deadline', event.target.value)}
                    required
                    className={errors.deadline ? 'sf-input-invalid' : undefined}
                    aria-invalid={Boolean(errors.deadline)}
                    aria-describedby={
                      errors.deadline ? 'project-deadline-error' : undefined
                    }
                  />
                  <svg viewBox="0 0 24 24" aria-hidden="true">
                    <rect x="3.5" y="5" width="17" height="16" rx="2" />
                    <path d="M7.5 3v4M16.5 3v4M4 9.5h16" />
                  </svg>
                </div>
                {errors.deadline && (
                  <span id="project-deadline-error" className="sf-error-message" role="alert">
                    {errors.deadline}
                  </span>
                )}
              </div>
            </section>

            <div className="sf-form-divider" />

            <section
              className="sf-form-section sf-team-section"
              aria-labelledby="team-section-title"
            >
              <SectionHeading id="team-section-title">2 · Tým</SectionHeading>

              <div className="sf-field">
                <label htmlFor="team-name">
                  Název týmu <span>*</span>
                </label>
                <input
                  id="team-name"
                  name="teamName"
                  type="text"
                  placeholder="Např. Tým Sova"
                  value={values.teamName}
                  onChange={(event) => update('teamName', event.target.value)}
                  maxLength={50}
                  required
                  className={errors.teamName ? 'sf-input-invalid' : undefined}
                  aria-invalid={Boolean(errors.teamName)}
                  aria-describedby={errors.teamName ? 'team-name-error' : undefined}
                />
                {errors.teamName && (
                  <span id="team-name-error" className="sf-error-message" role="alert">
                    {errors.teamName}
                  </span>
                )}
              </div>

              <div className="sf-info-note">
                <svg viewBox="0 0 24 24" aria-hidden="true">
                  <circle cx="12" cy="12" r="9" />
                  <path d="M12 11v5M12 8h.01" />
                </svg>
                <span>
                  Členy týmu a vyučujícího přidáte po založení v Nastavení projektu.
                </span>
              </div>
            </section>

            {serverError && (
              <div className="sf-error-message" role="alert">
                {serverError}
              </div>
            )}

            <div className="sf-form-actions">
              <button
                className="sf-button sf-button-secondary"
                type="button"
                onClick={handleCancel}
                disabled={isSubmitting}
              >
                Zrušit
              </button>
              <button
                className="sf-button sf-button-primary"
                type="submit"
                disabled={isSubmitting}
              >
                {isSubmitting ? 'Zakládání projektu…' : 'Založit projekt'}
              </button>
            </div>
          </form>
        </div>
      </section>
    </Container>
  )
}