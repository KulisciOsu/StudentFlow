import { afterEach, describe, expect, it, vi } from 'vitest'
import { saveTaskMove } from './kanbanTasksApi'

const csrf = () => new Response(JSON.stringify({ token: 'token-1', headerName: 'X-CSRF-TOKEN' }),
  { status: 200, headers: { 'Content-Type': 'application/json' } })
afterEach(() => vi.unstubAllGlobals())

describe('saveTaskMove', () => {
  it('sends the move with a CSRF token', async () => {
    const fetch = vi.fn().mockResolvedValueOnce(csrf()).mockResolvedValueOnce(new Response(null, { status: 204 }))
    vi.stubGlobal('fetch', fetch)

    await saveTaskMove(1, 4, 3, 0)

    expect(fetch.mock.calls[0][0]).toBe('/api/auth/csrf')
    const [path, options] = fetch.mock.calls[1]
    expect(path).toBe('/api/projects/1/tasks/4/position')
    expect(options.method).toBe('PUT')
    expect(options.headers['X-CSRF-TOKEN']).toBe('token-1')
    expect(JSON.parse(options.body)).toEqual({ columnId: 3, position: 0 })
  })

  it('rejects when the server refuses the move', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValueOnce(csrf()).mockResolvedValueOnce(new Response(null, { status: 403 })))

    await expect(saveTaskMove(1, 4, 3, 0)).rejects.toThrow('Nepodařilo se uložit přesun úkolu.')
  })
})
