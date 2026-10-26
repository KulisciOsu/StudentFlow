import { useState } from 'react'
import { LoginPage } from './components/auth/login/LoginPage'
import { RegisterPage } from './components/auth/register/RegisterPage'
import { KanbanBoard } from './components/kanban/board/KanbanBoard'
import { CreateProjectPage } from './components/projects/create/CreateProjectPage'
import { MyProjectsPage } from './components/projects/my-projects/MyProjectsPage'
import type { AuthUser } from './models/auth/AuthTypes'
import './App.scss'

/// <summary>
/// Zobrazení aplikace. Dočasné řešení, než bude přidáno směrování.
/// </summary>
type AppView = 'login' | 'register' | 'my-projects' | 'create-project' | 'board'

/// <summary>
/// Řídí přechody mezi obrazovkami aplikace StudentFlow.
/// </summary>
function App() {
  const [view, setView] = useState<AppView>('login')
  const [user, setUser] = useState<AuthUser | null>(null)
  const [justRegistered, setJustRegistered] = useState(false)

  /// <summary>
  /// Uloží přihlášeného uživatele a otevře jeho úvodní obrazovku.
  /// </summary>
  function handleLoggedIn(loggedInUser: AuthUser) {
    setUser(loggedInUser)
    setJustRegistered(false)
    // TODO: vyučující patří na Přehled vyučujícího (UC06, UC07).
    setView(loggedInUser.accountType === 'student' ? 'my-projects' : 'board')
  }

  return (
    <main className="app">
      <h1>StudentFlow</h1>
      {user && <p className="app__user">Přihlášen: {user.name}</p>}

      {view === 'login' && (
        <LoginPage
          notice={justRegistered ? 'Účet byl vytvořen. Nyní se můžete přihlásit.' : undefined}
          onLoggedIn={handleLoggedIn}
          onGoToRegister={() => setView('register')}
        />
      )}
      {view === 'register' && (
        <RegisterPage
          onRegistered={() => {
            setJustRegistered(true)
            setView('login')
          }}
          onGoToLogin={() => setView('login')}
        />
      )}
      {view === 'my-projects' && (
        <MyProjectsPage
          onCreateProject={() => setView('create-project')}
          onOpenProject={() => setView('board')}
          onOpenTask={() => setView('board')}
        />
      )}
      {view === 'create-project' && (
        <CreateProjectPage
          onCreated={() => setView('board')}
          onCancel={() => setView('my-projects')}
        />
      )}
      {view === 'board' && (
        <>
          <button type="button" onClick={() => setView('my-projects')}>Zpět na Moje projekty</button>
          <KanbanBoard />
        </>
      )}
    </main>
  )
}

export default App
