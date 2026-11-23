import { describe, expect, it } from 'vitest'
import { formatMemberCount } from './memberCount'

describe('formatMemberCount', () => {
  it.each([
    [0, '0 členů'],
    [1, '1 člen'],
    [2, '2 členové'],
    [4, '4 členové'],
    [5, '5 členů'],
    [12, '12 členů'],
    [22, '22 členů'],
  ])('formats %i', (count, text) => {
    expect(formatMemberCount(count)).toBe(text)
  })
})
