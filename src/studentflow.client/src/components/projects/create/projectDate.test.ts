import { describe, expect, it } from 'vitest'
import { todayPragueDate } from './projectDate'

describe('todayPragueDate', () => {
  it.each([
    ['v zimě těsně před pražskou půlnocí', '2026-01-15T22:59:00Z', '2026-01-15'],
    ['v zimě těsně po pražské půlnoci', '2026-01-15T23:01:00Z', '2026-01-16'],
    ['v létě těsně před pražskou půlnocí', '2026-07-15T21:59:00Z', '2026-07-15'],
    ['v létě těsně po pražské půlnoci', '2026-07-15T22:01:00Z', '2026-07-16'],
  ])('%s', (_label, instant, expected) => {
    expect(todayPragueDate(new Date(instant))).toBe(expected)
  })
})
