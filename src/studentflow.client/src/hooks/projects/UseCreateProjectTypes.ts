import type { FormEvent } from 'react'
import type {
  CreatedProject, CreateProjectErrors, CreateProjectField, CreateProjectFormValues,
} from '../../models/projects/ProjectTypes'

/// <summary>
/// Stav odesílání formuláře.
/// </summary>
export type CreateProjectStatus = 'idle' | 'submitting' | 'error'

/// <summary>
/// Vstupní parametry hooku pro založení projektu.
/// </summary>
export interface UseCreateProjectOptions {
  /// <summary>
  /// Zavolá se po úspěšném založení projektu.
  /// </summary>
  onCreated: (project: CreatedProject) => void
}

/// <summary>
/// Data a operace formuláře pro založení projektu.
/// </summary>
export interface UseCreateProjectResult {
  /// <summary>
  /// Aktuální hodnoty formuláře.
  /// </summary>
  values: CreateProjectFormValues

  /// <summary>
  /// Chybové hlášky podle polí.
  /// </summary>
  errors: CreateProjectErrors

  /// <summary>
  /// Stav odesílání.
  /// </summary>
  status: CreateProjectStatus

  /// <summary>
  /// Obecná chyba odeslání, pokud nejde přiřadit k poli.
  /// </summary>
  submitError: string | null

  /// <summary>
  /// Změní hodnotu pole a smaže jeho chybu.
  /// </summary>
  setValue: (field: CreateProjectField, value: string) => void

  /// <summary>
  /// Ověří a odešle formulář.
  /// </summary>
  submit: (event: FormEvent<HTMLFormElement>) => Promise<void>
}
