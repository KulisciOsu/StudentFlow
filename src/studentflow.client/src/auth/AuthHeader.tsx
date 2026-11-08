import { useAuth } from './AuthContext'
import { homeFor } from './types'

/** Společná hlavička ověřeného účtu s navigací podle typu a serverovým odhlášením. */
export function AuthHeader({ navigate }: { navigate: (path: string) => void }) {
  const { state, signOut } = useAuth()
  if (state.status !== 'authenticated') return null
  const { user } = state.identity
  const home = homeFor(user)
  return <header className="session-header">
    <p><strong>{user.displayName}</strong><span>{user.accountType === 'STUDENT' ? 'Člen' : 'Vyučující'}</span></p>
    <nav aria-label="Účet">
      <a href={home} onClick={event => { event.preventDefault(); navigate(home) }}>Můj přehled</a>
      <a href="/board" onClick={event => { event.preventDefault(); navigate('/board') }}>Nástěnka</a>
      <button type="button" onClick={() => void signOut()}>Odhlásit se</button>
    </nav>
  </header>
}
