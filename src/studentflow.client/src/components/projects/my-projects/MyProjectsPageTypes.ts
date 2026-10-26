import type { MyTask } from '../../../models/projects/MyProjectsTypes'

/// <summary>
/// Vstupní parametry rozcestníku studenta.
/// </summary>
export interface MyProjectsPageProps {
  /// <summary>
  /// Přejde na založení nového projektu (UC04).
  /// </summary>
  onCreateProject: () => void

  /// <summary>
  /// Otevře projekt.
  /// </summary>
  onOpenProject: (projectId: number) => void

  /// <summary>
  /// Otevře detail úkolu.
  /// </summary>
  onOpenTask: (task: MyTask) => void
}
