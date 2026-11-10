import { describe, expect, it } from 'vitest'
import type { Task } from '../../models/tasks/TaskTypes'
import { moveTask } from './moveTask'

/// <summary>
/// Vytvoří testovací úkol.
/// </summary>
function task(id: number, columnId: number, position: number): Task {
  return { id, title: `Úkol ${id}`, columnId, position, priority: null, assignee: 'Jan Novák', dueDate: null, isMine: true, canMove: true }
}

const tasks = [task(1, 1, 0), task(2, 1, 1), task(3, 1, 2), task(4, 2, 0)]

/// <summary>
/// Vrátí úkoly sloupce ve tvaru „id@pozice“ seřazené podle pozice.
/// </summary>
function columnOrder(result: Task[], columnId: number) {
  return result
    .filter((item) => item.columnId === columnId)
    .sort((first, second) => first.position - second.position)
    .map((item) => `${item.id}@${item.position}`)
}

describe('moveTask', () => {
  it('přesune úkol do jiného sloupce a přečísluje oba sloupce', () => {
    const result = moveTask(tasks, 2, 2, 0)

    expect(columnOrder(result, 1)).toEqual(['1@0', '3@1'])
    expect(columnOrder(result, 2)).toEqual(['2@0', '4@1'])
  })

  it('změní pořadí úkolů ve stejném sloupci', () => {
    const result = moveTask(tasks, 1, 1, 2)

    expect(columnOrder(result, 1)).toEqual(['2@0', '3@1', '1@2'])
  })

  it('pozici mimo rozsah umístí na konec sloupce', () => {
    const result = moveTask(tasks, 1, 2, 99)

    expect(columnOrder(result, 2)).toEqual(['4@0', '1@1'])
  })

  it('neznámý úkol nechá beze změny', () => {
    expect(moveTask(tasks, 42, 1, 0)).toBe(tasks)
  })
})
