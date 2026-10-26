import { projectConfig } from '../../components/projects/projectConfig'
import type {
  CreateProjectErrors, CreateProjectFormValues, CreateProjectRequest,
} from '../../models/projects/ProjectTypes'
import { toIsoDate } from './dates'

/// <summary>
/// Ověří povinný text s nejvyšší délkou.
/// </summary>
function validateText(value: string, maxLength: number, requiredMessage: string, tooLongMessage: string) {
  const text = value.trim()
  if (text.length === 0) return requiredMessage
  if (text.length > maxLength) return tooLongMessage
  return undefined
}

/// <summary>
/// Ověří údaje formuláře pro založení projektu.
/// </summary>
export function validateCreateProject(
  values: CreateProjectFormValues,
  today: Date = new Date(),
): CreateProjectErrors {
  const { maxLength } = projectConfig
  const errors: CreateProjectErrors = {}

  const name = validateText(
    values.name, maxLength.name,
    'Zadejte název projektu.',
    `Název projektu může mít nejvýše ${maxLength.name} znaků.`,
  )
  if (name) errors.name = name

  if (values.description.trim().length > maxLength.description) {
    errors.description = `Popis může mít nejvýše ${maxLength.description} znaků.`
  }

  if (!/^\d{4}-\d{2}-\d{2}$/.test(values.deadline)) {
    errors.deadline = 'Zadejte plánovaný termín odevzdání.'
  } else if (values.deadline < toIsoDate(today)) {
    errors.deadline = 'Termín odevzdání nesmí být v minulosti.'
  }

  const teamName = validateText(
    values.teamName, maxLength.teamName,
    'Zadejte název týmu.',
    `Název týmu může mít nejvýše ${maxLength.teamName} znaků.`,
  )
  if (teamName) errors.teamName = teamName

  return errors
}

/// <summary>
/// Převede hodnoty formuláře na požadavek pro API.
/// </summary>
export function toCreateProjectRequest(values: CreateProjectFormValues): CreateProjectRequest {
  const description = values.description.trim()

  return {
    name: values.name.trim(),
    description: description.length > 0 ? description : null,
    deadline: values.deadline,
    teamName: values.teamName.trim(),
  }
}
