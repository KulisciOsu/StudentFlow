// @vitest-environment jsdom
import { act, cleanup, fireEvent, render, screen } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import type { ProjectMember } from '../../../models/projects/ProjectMemberTypes'
import { addProjectMember, ProjectMembersFailure } from '../../../services/projects/projectMembersApi'
import { AddMemberForm } from './AddMemberForm'

const { refresh } = vi.hoisted(() => ({ refresh: vi.fn() }))

vi.mock('../../../auth/AuthContext', () => ({ useAuth: () => ({ refresh }) }))
vi.mock('../../../services/projects/projectMembersApi', async importOriginal => ({
  ...await importOriginal<typeof import('../../../services/projects/projectMembersApi')>(),
  addProjectMember: vi.fn(),
}))

const lucie: ProjectMember = {
  userId: '3', displayName: 'Lucie Černá', email: 'lucie@example.test', isLeader: false, joinedAt: '2026-11-15T10:00:00Z',
}
const onChanged = vi.fn()
const onSubmitStart = vi.fn()
const onAdded = vi.fn()

/// <summary>
/// Vykreslí formulář pro projekt 7 a vrátí jeho pole a tlačítko.
/// </summary>
function renderForm() {
  render(<AddMemberForm projectId={7} onChanged={onChanged} onSubmitStart={onSubmitStart} onAdded={onAdded} />)
  return {
    input: screen.getByRole<HTMLInputElement>('textbox', { name: 'E-mail registrovaného studenta' }),
    button: screen.getByRole<HTMLButtonElement>('button', { name: 'Přidat do týmu' }),
  }
}

/// <summary>
/// Vyplní e-mail a odešle formulář.
/// </summary>
async function submit(input: HTMLInputElement, email: string) {
  fireEvent.change(input, { target: { value: email } })
  await act(async () => { fireEvent.submit(input.form!) })
}

beforeEach(() => {
  refresh.mockReset().mockResolvedValue(undefined)
  onChanged.mockReset()
  onSubmitStart.mockReset()
  onAdded.mockReset()
  vi.mocked(addProjectMember).mockReset()
})
afterEach(cleanup)

describe('AddMemberForm', () => {
  it('shows the wireframe texts', () => {
    const { input } = renderForm()

    expect(screen.getByRole('heading', { name: 'Přidat člena' })).toBeTruthy()
    expect(input.placeholder).toBe('E-mail registrovaného studenta')
    expect(screen.getByText('Člen musí mít v StudentFlow účet. Odebraný člen ztratí přístup, jeho příspěvky zůstanou zachované.')).toBeTruthy()
  })

  it('asks for an e-mail without calling the API when the field is empty', async () => {
    const { input } = renderForm()

    await submit(input, '   ')

    expect(screen.getByRole('alert').textContent).toBe('Zadejte e-mail studenta.')
    expect(addProjectMember).not.toHaveBeenCalled()
    expect(onSubmitStart).toHaveBeenCalledTimes(1)
  })

  it('disables the button while sending, then clears the field, reports the new member and reloads the list', async () => {
    let resolve: (member: ProjectMember) => void = () => {}
    vi.mocked(addProjectMember).mockReturnValueOnce(new Promise(done => { resolve = done }))
    const { input, button } = renderForm()

    await submit(input, ' lucie@example.test ')

    expect(button.disabled).toBe(true)
    expect(button.textContent).toBe('Přidáváme…')
    // Pole typu email okolní mezery odstraní samo, stejně jako prohlížeč.
    expect(vi.mocked(addProjectMember).mock.calls[0].slice(0, 2)).toEqual([7, 'lucie@example.test'])
    await act(async () => resolve(lucie))
    expect(input.value).toBe('')
    expect(button.disabled).toBe(false)
    expect(onSubmitStart).toHaveBeenCalledTimes(1)
    expect(onAdded).toHaveBeenCalledWith(lucie)
    // Potvrzení zobrazuje sekce členů, formulář sám žádnou zprávu o úspěchu nemá.
    expect(screen.queryByRole('status')).toBeNull()
    expect(onChanged).toHaveBeenCalledTimes(1)
  })

  it.each([
    [422, 'ACCOUNT_NOT_FOUND', 'Účet s tímto e-mailem v StudentFlow neexistuje.'],
    [422, 'ACCOUNT_NOT_STUDENT', 'Do týmu lze přidat jen účet studenta.'],
    [409, 'MEMBER_ALREADY_EXISTS', 'Tento uživatel už je členem týmu.'],
    [400, 'VALIDATION_FAILED', 'Zadejte platnou e-mailovou adresu, nejvýše 320 znaků.'],
  ])('shows %i %s, keeps the typed e-mail and does not reload', async (status, code, message) => {
    vi.mocked(addProjectMember).mockRejectedValueOnce(new ProjectMembersFailure(message, status, code))
    const { input } = renderForm()

    await submit(input, 'nikdo@example.test')

    expect(screen.getByRole('alert').textContent).toBe(message)
    expect(input.value).toBe('nikdo@example.test')
    expect(input.getAttribute('aria-invalid')).toBe('true')
    expect(onAdded).not.toHaveBeenCalled()
    expect(onChanged).not.toHaveBeenCalled()
  })

  it('reloads the page state when the leader lost access meanwhile', async () => {
    vi.mocked(addProjectMember).mockRejectedValueOnce(
      new ProjectMembersFailure('Členy projektu může spravovat jen vedoucí týmu.', 403, 'PROJECT_LEADER_REQUIRED'))
    const { input } = renderForm()

    await submit(input, 'lucie@example.test')

    expect(screen.getByRole('alert').textContent).toBe('Členy projektu může spravovat jen vedoucí týmu.')
    expect(onChanged).toHaveBeenCalledTimes(1)
  })

  it('reports a connection failure and reloads, because the member may have been added', async () => {
    vi.mocked(addProjectMember).mockRejectedValueOnce(new TypeError('Failed to fetch'))
    const { input } = renderForm()

    await submit(input, 'lucie@example.test')

    expect(screen.getByRole('alert').textContent).toBe('Člena se nepodařilo přidat. Zkontrolujte připojení a zkuste to znovu.')
    expect(onChanged).toHaveBeenCalledTimes(1)
  })

  it('lets the app re-check the session after 401', async () => {
    vi.mocked(addProjectMember).mockRejectedValueOnce(new ProjectMembersFailure('Vaše přihlášení vypršelo.', 401, 'UNAUTHENTICATED'))
    const { input } = renderForm()

    await submit(input, 'lucie@example.test')

    expect(refresh).toHaveBeenCalledTimes(1)
    expect(screen.queryByRole('alert')).toBeNull()
  })
})
