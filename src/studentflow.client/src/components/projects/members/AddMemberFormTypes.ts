import type { ProjectMember } from '../../../models/projects/ProjectMemberTypes'

/// <summary>
/// Vlastnosti formuláře „Přidat člena“.
/// </summary>
export interface AddMemberFormProps {
  /// <summary>
  /// Projekt, do kterého se člen přidává.
  /// </summary>
  projectId: number

  /// <summary>
  /// Zavolá se, když se členové na serveru mohli změnit; stránka pak seznam znovu načte.
  /// </summary>
  onChanged: () => void

  /// <summary>
  /// Zavolá se při každém odeslání formuláře, aby sekce smazala zprávu o předchozí akci.
  /// </summary>
  onSubmitStart: () => void

  /// <summary>
  /// Zavolá se po úspěšném přidání; potvrzení zobrazí sekce členů.
  /// </summary>
  onAdded: (member: ProjectMember) => void
}
