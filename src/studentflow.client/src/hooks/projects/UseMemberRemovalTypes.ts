import type { ProjectMember } from '../../models/projects/ProjectMemberTypes'

/// <summary>
/// Výsledek hooku useMemberRemoval: stav potvrzení a odebírání a akce pro tlačítka v řádcích.
/// </summary>
export interface UseMemberRemovalResult {
  /// <summary>
  /// ID člena, u jehož řádku je otevřené potvrzení „Opravdu odebrat?“, jinak null.
  /// </summary>
  confirmingUserId: string | null

  /// <summary>
  /// True, dokud běží požadavek na odebrání; všechna tlačítka pro odebrání jsou mezitím vypnutá.
  /// </summary>
  busy: boolean

  /// <summary>
  /// Chybová zpráva posledního pokusu o odebrání, nebo null.
  /// </summary>
  error: string | null

  /// <summary>
  /// Potvrzení posledního úspěšného odebrání, nebo null.
  /// </summary>
  success: string | null

  /// <summary>
  /// Otevře potvrzení u daného člena; případné jiné otevřené potvrzení zavře.
  /// </summary>
  ask: (userId: string) => void

  /// <summary>
  /// Zavře potvrzení bez odebrání.
  /// </summary>
  cancel: () => void

  /// <summary>
  /// Odešle odebrání potvrzeného člena na server.
  /// </summary>
  confirm: (member: ProjectMember) => Promise<void>
}
