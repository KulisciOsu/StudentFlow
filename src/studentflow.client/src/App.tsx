import { useCallback, useEffect, useState } from 'react'
import { RegistrationPage } from './auth/RegistrationPage'
import { LoginPage } from './auth/LoginPage'
import { AuthHeader } from './auth/AuthHeader'
import { useAuth } from './auth/AuthContext'
import { homeFor } from './auth/types'
import { CreateProjectPage } from './components/projects/create/CreateProjectPage'
import { Dashboard } from './components/dashboard/Dashboard'
import './App.scss'

type Route = { path: string; email?: string; notice?: string }
type Navigate = (path: string, options?: { email?: string; notice?: string; replace?: boolean }) => void

function Redirect({ to, navigate }: { to: string; navigate: Navigate }) {
  useEffect(() => navigate(to, { replace: true }), [to, navigate])
  return <p role="status">Přesměrováváme…</p>
}

/** Volí stránky podle ověřené identity a typu účtu; během ověřování či odhlášení chráněný obsah skryje. */
function App() {
  const { state, refresh, signOut } = useAuth()
  const [route, setRoute] = useState<Route>({ path: window.location.pathname })
  const navigate = useCallback<Navigate>((path, options = {}) => {
    // E-mail z registrace zůstává pouze v paměti, nikoli v URL, historii či storage.
    window.history[options.replace ? 'replaceState' : 'pushState'](null, '', path)
    setRoute({ path, email: options.email, notice: options.notice })
  }, [])
  useEffect(() => {
    const back = () => setRoute({ path: window.location.pathname })
    window.addEventListener('popstate', back)
    return () => window.removeEventListener('popstate', back)
  }, [])
  useEffect(() => {
    const heading = document.querySelector<HTMLElement>('main h2')
    heading?.setAttribute('tabindex', '-1'); heading?.focus()
  }, [route.path, state.status])

  let content
  if (state.status === 'loading') content = <p role="status">Ověřujeme přihlášení…</p>
  else if (state.status === 'error') content = <section><h2>Přihlášení se nepodařilo ověřit</h2>
    <p role="alert">Zkontrolujte připojení a zkuste to znovu.</p><button onClick={() => void refresh()}>Zkusit znovu</button></section>
  else if (state.status === 'loggingOut') content = <p role="status">Odhlašujeme…</p>
  else if (state.status === 'logoutError') content = <section><h2>Odhlášení se nepodařilo potvrdit</h2>
    <p role="alert">Zkontrolujte připojení a zkuste to znovu.</p><button onClick={() => void signOut()}>Zkusit odhlášení znovu</button></section>
  else if (state.status === 'anonymous') {
    if (route.path === '/register') content = <RegistrationPage onLogin={() => navigate('/login')}
      onRegistered={user => navigate('/login', { email: user.email, notice: 'Účet byl vytvořen. Nyní se přihlaste.' })} />
    else if (route.path === '/login') content = <LoginPage initialEmail={route.email}
      notice={route.notice ?? (state.notice === 'expired' ? 'Přihlášení již neplatí. Přihlaste se znovu.' : state.notice === 'loggedOut' ? 'Byli jste odhlášeni.' : undefined)}
      onRegister={() => navigate('/register')} />
    else content = <Redirect to="/login" navigate={navigate} />
  } else if (state.status === 'authenticated') {
    const { user } = state.identity
    const home = homeFor(user)
    if (route.path === '/' || route.path === '/login' || route.path === '/register') content = <Redirect to={home} navigate={navigate} />
    else {
      const creatingProject = route.path === '/projects/new'
      const wrongType = ((route.path === '/projects' || route.path === '/teaching') && route.path !== home)
        || (creatingProject && user.accountType !== 'STUDENT')
      content = <>
        <AuthHeader navigate={navigate} />
        {wrongType ? <section><h2>Přístup odepřen</h2><p>Tento pohled není určen pro váš typ účtu.</p>
          <a href={home} onClick={event => { event.preventDefault(); navigate(home) }}>Přejít na můj přehled</a></section>
          : creatingProject ? <CreateProjectPage embedded onBack={() => navigate(home)} />
            : route.path === home ? user.accountType === 'STUDENT'
              ? <Dashboard embedded onCreateProject={() => navigate('/projects/new')} />
              : <section><h2>Přehled vyučujícího</h2><p>Zde budete mít přehled projektů, ke kterým dostanete přístup.</p></section>
            : <section><h2>Stránka nebyla nalezena</h2><a href={home} onClick={event => { event.preventDefault(); navigate(home) }}>Přejít na můj přehled</a></section>}
      </>
    }
  }
  const formLayout = state.status === 'anonymous' && route.path === '/register' ? ' app--register'
    : state.status === 'anonymous' && route.path === '/login' ? ' app--login' : ''
  return <main className={`app ${state.status === 'authenticated' ? 'app--workspace' : 'app--auth'}${formLayout}`}>
    <h1>StudentFlow</h1>{content}
  </main>
}

export default App
