import type { CreatedProject } from '../../../models/projects/ProjectTypes'

/// <summary>
/// Vstupní parametry formuláře pro založení projektu.
/// </summary>
export interface CreateProjectFormProps {
  /// <summary>
  /// Zavolá se po úspěšném založení projektu.
  /// </summary>
  onCreated: (project: CreatedProject) => void

  /// <summary>
  /// Zavolá se při zrušení formuláře.
  /// </summary>
  onCancel: () => void
}
