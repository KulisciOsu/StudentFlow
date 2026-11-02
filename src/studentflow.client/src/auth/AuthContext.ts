import { createContext, useContext } from 'react'
import type { LoginFields } from './session'
import type { Identity } from './types'

export type AuthState = { status: 'loading' | 'error' | 'loggingOut' | 'logoutError' }
  | { status: 'anonymous'; notice?: 'expired' | 'loggedOut' }
  | { status: 'authenticated'; identity: Identity }
export type AuthContextValue = { state: AuthState; refresh: () => Promise<void>; signIn: (fields: LoginFields, signal?: AbortSignal) => Promise<void>; signOut: () => Promise<void> }
export const AuthContext = createContext<AuthContextValue | null>(null)
export function useAuth() {
  const value = useContext(AuthContext)
  if (!value) throw new Error('AuthProvider is required.')
  return value
}
