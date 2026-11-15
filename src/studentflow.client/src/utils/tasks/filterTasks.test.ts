import { describe, expect, it } from 'vitest'
import type { Task } from '../../models/tasks/TaskTypes'
import { emptyTaskFilters, filterTasks } from './filterTasks'

const tasks: Task[] = [
  { id: 1, title: 'Návrh databázového modelu', columnId: 1, position: 0, priority: 'high', assignee: 'Jan Novák', dueDate: null, isMine: true, canMove: true },
  { id: 2, title: 'API pro výpůjčky', columnId: 1, position: 1, priority: 'medium', assignee: 'Martin Kučera', dueDate: null, isMine: false, canMove: false },
  { id: 3, title: 'Export výpůjček do CSV', columnId: 2, position: 0, priority: 'low', assignee: 'Martin Kučera', dueDate: null, isMine: false, canMove: false },
]

/// <summary>
/// Vrátí identifikátory úkolů odpovídajících filtrům.
/// </summary>
function ids(filters: Partial<typeof emptyTaskFilters>) {
  return filterTasks(tasks, { ...emptyTaskFilters, ...filters }).map((task) => task.id)
}

describe('filterTasks', () => {
  it('bez filtrů vrátí všechny úkoly', () => {
    expect(ids({})).toEqual([1, 2, 3])
  })

  it('hledá bez ohledu na velikost písmen a diakritiku', () => {
    expect(ids({ query: 'NAVRH' })).toEqual([1])
    expect(ids({ query: 'vypujc' })).toEqual([2, 3])
  })

  it('filtruje podle řešitele a priority', () => {
    expect(ids({ assignee: 'Martin Kučera' })).toEqual([2, 3])
    expect(ids({ priority: 'high' })).toEqual([1])
    expect(ids({ assignee: 'Martin Kučera', priority: 'low' })).toEqual([3])
  })

  it('filtr Jen moje úkoly ponechá úkoly přihlášeného uživatele', () => {
    expect(ids({ onlyMine: true })).toEqual([1])
    expect(ids({ onlyMine: true, priority: 'low' })).toEqual([])
  })

  it('bez shody vrátí prázdný seznam', () => {
    expect(ids({ query: 'neexistuje' })).toEqual([])
  })
})
