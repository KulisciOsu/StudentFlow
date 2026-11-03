
import { useState } from 'react'
import { KanbanBoard } from './components/kanban/board/KanbanBoard'
import { CreateProjectPage } from './components/projects/create/CreateProjectPage'
import './App.scss'

function App() {
  const [currentPage, setCurrentPage] = useState<'kanban' | 'create-project'>('kanban')

  if (currentPage === 'create-project') {
    return (
      <CreateProjectPage
        onBack={() => setCurrentPage('kanban')}
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
