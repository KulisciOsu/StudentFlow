/** Jediný typ účtu; STUDENT označuje člena, vedení projektu se řeší samostatně. */
export type AccountType = 'STUDENT' | 'TEACHER'
/** Veřejná identita bez tajemství; id zůstává řetězcem kvůli přesnosti databázových identifikátorů. */
export type AuthUser = { id: string; displayName: string; email: string; accountType: AccountType }
/** Identita ověřená serverem; expiresAt je pevný konec platnosti a persistent označuje trvalou cookie. */
export type Identity = { user: AuthUser; session: { expiresAt: string; persistent: boolean } }

export function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === 'object' && value !== null && !Array.isArray(value)
}
/** Ověří přesný veřejný tvar uživatele z nedůvěryhodného JSON před použitím v aplikaci. */
export function isAuthUser(value: unknown): value is AuthUser {
  return isRecord(value) && Object.keys(value).length === 4 && typeof value.id === 'string' && /^[1-9][0-9]*$/.test(value.id)
    && typeof value.displayName === 'string' && value.displayName.trim().length > 0
    && typeof value.email === 'string' && value.email.length > 0
    && (value.accountType === 'STUDENT' || value.accountType === 'TEACHER')
}
/** Ověří veřejnou identitu i metadata session včetně parsovatelného data expirace. */
export function isIdentity(value: unknown): value is Identity {
  return isRecord(value) && Object.keys(value).length === 2 && isAuthUser(value.user) && isRecord(value.session)
    && Object.keys(value.session).length === 2 && typeof value.session.expiresAt === 'string'
    && Number.isFinite(Date.parse(value.session.expiresAt)) && typeof value.session.persistent === 'boolean'
}
export const homeFor = (user: AuthUser) => user.accountType === 'STUDENT' ? '/projects' : '/teaching'
