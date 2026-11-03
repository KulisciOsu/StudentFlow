
import { useState } from 'react'
import { KanbanBoard } from './components/kanban/board/KanbanBoard'
import { CreateProjectPage } from './components/projects/create/CreateProjectPage'
import { Dashboard } from './components/dashboard/Dashboard'
import './App.scss'

function App() {
  const [currentPage, setCurrentPage] = useState<'dashboard' | 'kanban' | 'create-project'>('dashboard')
  
  if (currentPage === 'dashboard') {
  return <Dashboard onCreateProject={() => setCurrentPage('create-project')} />
  }

  if (currentPage === 'create-project') {
    return (
      <CreateProjectPage
        onBack={() => setCurrentPage('dashboard')}
      />
    )
  }

  return (
    <main className="app">
      <div className="app-header">
        <h1>StudentFlow</h1>
        <button
          className="app-create-project-button"
          type="button"
          onClick={() => setCurrentPage('create-project')}
        >
          Založit projekt
        </button>
      </div>

      <KanbanBoard />
    </main>
  )
}

export default App
