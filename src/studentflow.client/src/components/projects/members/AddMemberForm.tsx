import { useEffect, useRef, useState } from 'react'
import type { FormEvent } from 'react'
import { useAuth } from '../../../auth/AuthContext'
import { CsrfTokenError } from '../../../services/http/csrfRequest'
import { addProjectMember, ProjectMembersFailure } from '../../../services/projects/projectMembersApi'
import type { AddMemberFormProps } from './AddMemberFormTypes'
import './AddMemberForm.scss'

const connectionErrorMessage = 'Člena se nepodařilo přidat. Zkontrolujte připojení a zkuste to znovu.'

/// <summary>Chyby, po kterých se na serveru nic nezměnilo a seznam není nutné znovu načítat.</summary>
const inputErrorStatuses = [400, 409, 422]

/// <summary>
/// Formulář „Přidat člena“: vedoucí zadá e-mail registrovaného studenta.
/// </summary>
/// <remarks>
/// Lokálně kontroluje jen prázdné pole; formát e-mailu a všechna pravidla ověřuje server.
/// Chybu k zadanému e-mailu ukazuje u pole, potvrzení úspěchu předá sekci členů přes onAdded.
/// </remarks>
export function AddMemberForm({ projectId, onChanged, onSubmitStart, onAdded }: AddMemberFormProps) {
  const { refresh } = useAuth()
  const [email, setEmail] = useState('')
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const request = useRef<AbortController | null>(null)

  useEffect(() => () => request.current?.abort(), [])

  /// <summary>
  /// Odešle e-mail a podle výsledku vyčistí pole, ohlásí úspěch nebo ukáže chybu a nechá seznam znovu načíst.
  /// </summary>
  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (busy) return
    onSubmitStart()
    if (email.trim().length === 0) {
      setError('Zadejte e-mail studenta.')
      return
    }

    const controller = new AbortController()
    request.current = controller
    setBusy(true)
    setError(null)
    try {
      const member = await addProjectMember(projectId, email, controller.signal)
      if (controller.signal.aborted) return
      setEmail('')
      onAdded(member)
      onChanged()
    } catch (failure) {
      if (controller.signal.aborted) return
      if (failure instanceof ProjectMembersFailure && failure.status === 401) {
        await refresh()
        return
      }
      setError(failure instanceof ProjectMembersFailure || failure instanceof CsrfTokenError
        ? failure.message
        : connectionErrorMessage)
      // Po ztrátě oprávnění nebo nejistém výsledku ukáže stránka skutečný stav ze serveru.
      if (!(failure instanceof ProjectMembersFailure && inputErrorStatuses.includes(failure.status))) onChanged()
    } finally {
      if (!controller.signal.aborted) setBusy(false)
    }
  }

  return (
    <form className="add-member-form" onSubmit={submit} noValidate aria-labelledby="add-member-title">
      <h4 id="add-member-title" className="add-member-form__title">Přidat člena</h4>
      <div className="add-member-form__row">
        <input
          className="add-member-form__input"
          type="email"
          value={email}
          onChange={event => setEmail(event.target.value)}
          placeholder="E-mail registrovaného studenta"
          aria-label="E-mail registrovaného studenta"
          aria-describedby="add-member-help"
          aria-invalid={error !== null}
          autoComplete="off"
          maxLength={320}
        />
        <button className="add-member-form__submit" type="submit" disabled={busy}>
          {busy ? 'Přidáváme…' : 'Přidat do týmu'}
        </button>
      </div>
      {error && <p className="add-member-form__error" role="alert">{error}</p>}
      <p id="add-member-help" className="add-member-form__help">
        Člen musí mít v StudentFlow účet. Odebraný člen ztratí přístup, jeho příspěvky zůstanou zachované.
      </p>
    </form>
  )
}
