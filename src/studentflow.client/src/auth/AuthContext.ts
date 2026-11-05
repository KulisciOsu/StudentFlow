import { createContext, useContext } from 'react'
import type { LoginFields } from './session'
import type { Identity } from './types'

/**
 * Stav ověření session; chráněné stránky se zobrazují jen ve stavu authenticated.
 * error označuje nepotvrzenou identitu, logoutError nepotvrzené odhlášení na serveru.
 */
export type AuthState = { status: 'loading' | 'error' | 'loggingOut' | 'logoutError' }
  | { status: 'anonymous'; notice?: 'expired' | 'loggedOut' }
  | { status: 'authenticated'; identity: Identity }
/** Rozhraní sdílené identity a operací přihlášení pro komponenty aplikace. */
export type AuthContextValue = {
  state: AuthState;
  /** Znovu ověří session na serveru; během neukončeného odhlášení ověření nespouští. */
  refresh: () => Promise<void>;
  /** Po potvrzeném loginu uloží identitu; chybu předá formuláři jako odmítnutý Promise. */
  signIn: (fields: LoginFields, signal?: AbortSignal) => Promise<void>;
  /** Požádá server o zneplatnění session; výsledek včetně chyby promítne do state. */
  signOut: () => Promise<void>
}
export const AuthContext = createContext<AuthContextValue | null>(null)
/** Zpřístupní autentizaci; použití mimo AuthProvider považuje za chybu zapojení. */
export function useAuth() {
  const value = useContext(AuthContext)
  if (!value) throw new Error('AuthProvider is required.')
  return value
}
