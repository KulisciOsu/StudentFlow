import type { ProjectMember } from '../../../models/projects/ProjectMemberTypes'

/// <summary>
/// Vlastnosti sekce „Členové týmu“.
/// </summary>
export interface ProjectMembersProps {
  /// <summary>
  /// ID projektu, do kterého formulář přidává a ze kterého tlačítka „Odebrat“ odebírají členy.
  /// </summary>
  projectId: number

  /// <summary>
  /// Zavolá se po přidání nebo pokusu o odebrání, aby stránka znovu načetla seznam ze serveru.
  /// </summary>
  onChanged: () => void

  /// <summary>
  /// Název týmu zobrazený pod nadpisem.
  /// </summary>
  teamName: string

  /// <summary>
  /// Členové projektu, vedoucí jako první.
  /// </summary>
  members: ProjectMember[]

  /// <summary>
  /// ID přihlášeného uživatele; jeho řádek dostane označení „(vy)“.
  /// </summary>
  currentUserId: string
}
