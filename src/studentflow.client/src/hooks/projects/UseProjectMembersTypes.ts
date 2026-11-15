import type { ProjectMembers } from '../../models/projects/ProjectMemberTypes'

/// <summary>
/// Stav načtení členů projektu.
/// </summary>
export type ProjectMembersState =
  | { status: 'loading' }
  | { status: 'loaded'; data: ProjectMembers }
  | { status: 'error'; message: string; notFound: boolean }

/// <summary>
/// Výsledek hooku useProjectMembers.
/// </summary>
export interface UseProjectMembersResult {
  /// <summary>
  /// Aktuální stav: načítání, načtená data, nebo chyba se zprávou pro uživatele.
  /// </summary>
  state: ProjectMembersState

  /// <summary>
  /// Znovu načte seznam, například po přidání nebo odebrání člena; dosavadní data zůstanou vidět, než dorazí nová.
  /// </summary>
  reload: () => void
}
