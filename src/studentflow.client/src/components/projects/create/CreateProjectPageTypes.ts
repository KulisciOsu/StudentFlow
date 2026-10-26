import type { CreatedProject } from '../../../models/projects/ProjectTypes'

/// <summary>
/// Vstupní parametry obrazovky Založit projekt.
/// </summary>
export interface CreateProjectPageProps {
  /// <summary>
  /// Zavolá se po úspěšném založení projektu.
  /// </summary>
  onCreated: (project: CreatedProject) => void

  /// <summary>
  /// Vrátí se na rozcestník Moje projekty.
  /// </summary>
  onCancel: () => void
}
