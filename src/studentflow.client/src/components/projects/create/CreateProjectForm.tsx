import { useCreateProject } from '../../../hooks/projects/useCreateProject'
import { toIsoDate } from '../../../utils/projects/dates'
import { FormField } from '../../common/form-field/FormField'
import { projectConfig } from '../projectConfig'
import type { CreateProjectFormProps } from './CreateProjectFormTypes'
import './CreateProjectForm.scss'

/// <summary>
/// Zobrazuje formulář pro založení projektu a týmu.
/// </summary>
export function CreateProjectForm({ onCreated, onCancel }: CreateProjectFormProps) {
  const { values, errors, status, submitError, setValue, submit } = useCreateProject({ onCreated })
  const submitting = status === 'submitting'

  return (
    <form className="create-project-form" onSubmit={submit} noValidate aria-busy={submitting}>
      <fieldset disabled={submitting}>
        <legend>1 · Projekt</legend>
        <FormField
          id="project-name"
          label="Název projektu"
          required
          value={values.name}
          error={errors.name}
          maxLength={projectConfig.maxLength.name}
          onChange={(value) => setValue('name', value)}
        />
        <FormField
          id="project-description"
          label="Popis"
          multiline
          value={values.description}
          error={errors.description}
          placeholder="Stručně popište zadání projektu a jeho cíl."
          maxLength={projectConfig.maxLength.description}
          onChange={(value) => setValue('description', value)}
        />
        <FormField
          id="project-deadline"
          label="Plánovaný termín odevzdání"
          required
          type="date"
          min={toIsoDate(new Date())}
          value={values.deadline}
          error={errors.deadline}
          onChange={(value) => setValue('deadline', value)}
        />
      </fieldset>

      <hr />

      <fieldset disabled={submitting}>
        <legend>2 · Tým</legend>
        <FormField
          id="team-name"
          label="Název týmu"
          required
          value={values.teamName}
          error={errors.teamName}
          placeholder="Např. Tým Sova"
          maxLength={projectConfig.maxLength.teamName}
          onChange={(value) => setValue('teamName', value)}
        />
        <p className="create-project-form__note">
          <span aria-hidden="true">i</span>
          Členy týmu a vyučujícího přidáte po založení v Nastavení projektu.
        </p>
      </fieldset>

      {submitError && <p className="create-project-form__error" role="alert">{submitError}</p>}

      <div className="create-project-form__actions">
        <button type="button" className="create-project-form__cancel" onClick={onCancel} disabled={submitting}>
          Zrušit
        </button>
        <button type="submit" className="create-project-form__submit" disabled={submitting}>
          {submitting ? 'Zakládám…' : 'Založit projekt'}
        </button>
      </div>
    </form>
  )
}
