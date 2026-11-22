
import { useAuth } from './AuthContext'

/**
 * Společná hlavička ověřeného účtu se jménem, typem účtu a serverovým odhlášením.
 * Cestu zpět na přehled nabízí nadpis aplikace a drobečková navigace jednotlivých stránek.
 */
export function AuthHeader() {
  const { state, signOut } = useAuth()

  if (state.status !== 'authenticated') return null

  const { user } = state.identity

  return (
    <header className="session-header">
      <p>
        <strong>{user.displayName}</strong>
        <span>{user.accountType === 'STUDENT' ? 'Člen' : 'Vyučující'}</span>
      </p>

      <button type="button" onClick={() => void signOut()}>
        Odhlásit se
      </button>
    </header>
  )
}
