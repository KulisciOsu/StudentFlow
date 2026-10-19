import { KanbanBoard } from './components/kanban/board/KanbanBoard'
import './App.scss'

/// <summary>
/// Zobrazuje Kanban nástěnku jako hlavní obsah aplikace StudentFlow.
/// </summary>
function App() {
  return (
    <main className="app">
      <h1>StudentFlow</h1>
      <KanbanBoard />
    </main>
  )
}

export default App
