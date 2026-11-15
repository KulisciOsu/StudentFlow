import type { ProjectMember } from '../../models/projects/ProjectMemberTypes'

/// <summary>
/// Zpráva o výsledku poslední akce v sekci „Členové týmu“ (přidání nebo odebrání člena).
/// </summary>
export interface MemberNotice {
  /// <summary>
  /// „success“ se zobrazí jako stav (role status), „error“ jako upozornění (role alert).
  /// </summary>
  kind: 'success' | 'error'

  /// <summary>
  /// Text pro uživatele.
  /// </summary>
  text: string
}

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
