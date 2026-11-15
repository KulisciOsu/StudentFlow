import { describe, expect, it } from 'vitest'
import { memberInitials } from './memberInitials'

describe('memberInitials', () => {
  it.each([
    ['Petra Svobodová', 'PS'],
    ['Lucie Černá', 'LČ'],
    ['  jan   novák ', 'JN'],
    ['Anna Marie Nováková', 'AN'],
    ['Jan', 'J'],
    ['čeněk', 'Č'],
    ['🦉 Sova', '🦉S'],
    ['', '?'],
    ['   ', '?'],
  ])('returns initials for "%s"', (name, initials) => {
    expect(memberInitials(name)).toBe(initials)
  })
})
