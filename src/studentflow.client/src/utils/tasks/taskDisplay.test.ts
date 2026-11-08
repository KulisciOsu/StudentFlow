import { afterEach, describe, expect, it, vi } from 'vitest'
import { formatDate, formatShortDate, getInitials, isOverdue } from './taskDisplay'

afterEach(() => {
  vi.useRealTimers()
})

describe('taskDisplay', () => {
  it('formátuje datum česky', () => {
    expect(formatShortDate('2026-10-22')).toBe('22. 10.')
    expect(formatDate('2027-01-15')).toBe('15. 1. 2027')
  })

  it('termín je po termínu až po uplynutí dne', () => {
    vi.useFakeTimers()
    vi.setSystemTime(new Date(2026, 10, 2, 12))

    expect(isOverdue('2026-11-01')).toBe(true)
    expect(isOverdue('2026-11-02')).toBe(false)
    expect(isOverdue('2026-11-03')).toBe(false)
    expect(isOverdue(null)).toBe(false)
  })

  it('vrací iniciály jména a příjmení', () => {
    expect(getInitials('Lucie Černá')).toBe('LČ')
    expect(getInitials('Jan Karel Novák')).toBe('JN')
    expect(getInitials('Eva')).toBe('E')
  })
})
