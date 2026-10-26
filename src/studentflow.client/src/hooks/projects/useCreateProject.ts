import { useEffect, useRef, useState } from 'react'
import type { FormEvent } from 'react'
import type {
  CreateProjectErrors, CreateProjectField, CreateProjectFormValues,
} from '../../models/projects/ProjectTypes'
import { CreateProjectValidationError, createProject } from '../../services/projects/projectsApi'
import { toCreateProjectRequest, validateCreateProject } from '../../utils/projects/createProjectValidation'
import type {
  CreateProjectStatus, UseCreateProjectOptions, UseCreateProjectResult,
} from './UseCreateProjectTypes'

const emptyValues: CreateProjectFormValues = {
  name: '',
  description: '',
  deadline: '',
  teamName: '',
}

/// <summary>
/// Spravuje formulář pro založení projektu a jeho odeslání do API.
/// </summary>
export function useCreateProject({ onCreated }: UseCreateProjectOptions): UseCreateProjectResult {
  const [values, setValues] = useState<CreateProjectFormValues>(emptyValues)
  const [errors, setErrors] = useState<CreateProjectErrors>({})
  const [status, setStatus] = useState<CreateProjectStatus>('idle')
  const [submitError, setSubmitError] = useState<string | null>(null)
  const controllerRef = useRef<AbortController | null>(null)

  useEffect(() => () => controllerRef.current?.abort(), [])

  /// <summary>
  /// Změní hodnotu pole a smaže jeho chybu.
  /// </summary>
  function setValue(field: CreateProjectField, value: string) {
    setValues((current) => ({ ...current, [field]: value }))
    setErrors((current) => ({ ...current, [field]: undefined }))
  }

  /// <summary>
  /// Ověří a odešle formulář.
  /// </summary>
  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (status === 'submitting') return

    const validationErrors = validateCreateProject(values)
    setErrors(validationErrors)
    setSubmitError(null)
    if (Object.keys(validationErrors).length > 0) return

    const controller = new AbortController()
    controllerRef.current = controller
    setStatus('submitting')

    try {
      const project = await createProject(toCreateProjectRequest(values), controller.signal)
      if (controller.signal.aborted) return
      onCreated(project)
    } catch (error) {
      if (controller.signal.aborted) return

      if (error instanceof CreateProjectValidationError) {
        setErrors(error.fieldErrors)
      } else {
        setSubmitError('Projekt se nepodařilo založit. Zkuste to prosím znovu.')
      }
      setStatus('error')
    }
  }

  return { values, errors, status, submitError, setValue, submit }
}
