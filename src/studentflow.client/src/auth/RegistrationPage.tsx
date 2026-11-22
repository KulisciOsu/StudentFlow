import { useEffect, useRef, useState } from 'react'
import type { FormEvent } from 'react'
import { registerAccount, RegistrationFailure, validateRegistration } from './registration'
import type { RegisteredUser, RegistrationErrors, RegistrationFields } from './registration'
import './RegistrationPage.scss'
import { privacyPolicyUrl } from './privacy'

const initialFields: RegistrationFields = { firstName: '', lastName: '', email: '', password: '', passwordConfirmation: '', accountType: '', privacyAccepted: false }
const labels: Record<keyof RegistrationFields, string> = {
  firstName: 'Jméno', lastName: 'Příjmení', email: 'E-mail', password: 'Heslo', passwordConfirmation: 'Heslo znovu', accountType: 'Typ účtu',
  privacyAccepted: 'Ochrana osobních údajů',
}

/** Formulář pro jeden typ účtu; onRegistered volá až po potvrzení založení účtu serverem. */
export function RegistrationPage({ onRegistered, onLogin }: { onRegistered: (user: RegisteredUser) => void; onLogin: () => void }) {
  const [fields, setFields] = useState(initialFields)
  const [errors, setErrors] = useState<RegistrationErrors>({})
  const [message, setMessage] = useState('')
  const [busy, setBusy] = useState(false)
  const [showPassword, setShowPassword] = useState(false)
  const [showConfirmation, setShowConfirmation] = useState(false)
  const [errorRevision, setErrorRevision] = useState(0)
  const [retryUntil, setRetryUntil] = useState(0)
  const [remaining, setRemaining] = useState(0)
  const summary = useRef<HTMLDivElement>(null)
  const request = useRef<AbortController | null>(null)
  const inFlight = useRef(false)

  useEffect(() => () => request.current?.abort(), [])
  useEffect(() => { if (errorRevision > 0) summary.current?.focus() }, [errorRevision])
  useEffect(() => {
    if (!retryUntil) return
    const tick = () => setRemaining(Math.max(0, Math.ceil((retryUntil - Date.now()) / 1000)))
    const interval = window.setInterval(tick, 1000)
    return () => window.clearInterval(interval)
  }, [retryUntil])

  function update<K extends keyof RegistrationFields>(field: K, value: RegistrationFields[K]) {
    setFields(current => ({ ...current, [field]: value }))
    setErrors(current => ({ ...current, [field]: undefined }))
  }

  /** Odešle validní údaje bez souběžných pokusů; při chybě smaže hesla a zohlední čekání z Retry-After. */
  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (inFlight.current || Date.now() < retryUntil) return
    const validation = validateRegistration(fields)
    setErrors(validation)
    if (Object.keys(validation).length) {
      setMessage('Ještě prosím zkontrolujte označené údaje.')
      setErrorRevision(current => current + 1)
      return
    }
    const controller = new AbortController()
    request.current = controller
    inFlight.current = true
    setBusy(true)
    setMessage('')
    try {
      const user = await registerAccount(fields, controller.signal)
      if (!controller.signal.aborted) {
        setFields(initialFields)
        onRegistered(user)
      }
    } catch (error) {
      if (!controller.signal.aborted) {
        setFields(current => ({ ...current, password: '', passwordConfirmation: '' }))
        setErrors(error instanceof RegistrationFailure ? error.errors : {})
        setMessage(error instanceof RegistrationFailure ? error.message : 'Vytvoření účtu se nepodařilo potvrdit. Zkontrolujte připojení a zkuste se nejdřív přihlásit; účet už mohl vzniknout.')
        if (error instanceof RegistrationFailure && error.retryAfter) {
          setRetryUntil(Date.now() + error.retryAfter * 1000)
          setRemaining(error.retryAfter)
        }
        setErrorRevision(current => current + 1)
      }
    } finally {
      inFlight.current = false
      if (!controller.signal.aborted) setBusy(false)
    }
  }

  const errorText = (field: keyof RegistrationFields) => errors[field]
    ? <p id={`register-${field}-error`} className="field-error">{errors[field]}</p> : null
  const input = (field: 'firstName' | 'lastName' | 'email' | 'password' | 'passwordConfirmation') => {
    const isPassword = field === 'password' || field === 'passwordConfirmation'
    const show = field === 'password' ? showPassword : showConfirmation
    const toggle = () => field === 'password' ? setShowPassword(!showPassword) : setShowConfirmation(!showConfirmation)
    return <div className="registration-field" key={field}>
      <label htmlFor={`register-${field}`}>{labels[field]} <span aria-hidden="true">*</span></label>
      <div className={isPassword ? 'password-input' : undefined}>
        <input id={`register-${field}`} name={field} required
          type={isPassword ? (show ? 'text' : 'password') : field === 'email' ? 'email' : 'text'}
          autoComplete={isPassword ? 'new-password' : field === 'email' ? 'username' : field === 'firstName' ? 'given-name' : 'family-name'}
          autoCapitalize={field === 'email' ? 'none' : undefined} spellCheck={isPassword || field === 'email' ? false : undefined}
          value={fields[field]} onChange={event => update(field, event.target.value)}
          aria-invalid={!!errors[field]}
          aria-describedby={[field === 'password' ? 'password-hint' : '', errors[field] ? `register-${field}-error` : ''].filter(Boolean).join(' ') || undefined}
        />
        {isPassword && <button type="button" onClick={toggle} aria-pressed={show}
          aria-label={`${show ? 'Skrýt' : 'Zobrazit'} ${field === 'password' ? 'heslo' : 'opakované heslo'}`}>
          {show ? 'Skrýt' : 'Zobrazit'}
        </button>}
      </div>
      {field === 'password' && <p className="field-hint" id="password-hint">8–15 znaků, alespoň jedno velké písmeno, číslice a speciální znak (např. ! nebo #). Bez mezer.</p>}
      {errorText(field)}
    </div>
  }

  return <section aria-labelledby="registration-title">
    <h2 id="registration-title" tabIndex={-1}>Registrace</h2>
    <p>Vytvořte si účet pro spolupráci na studentských projektech.</p>
    <p className="field-hint">Pole označená * jsou povinná.</p>
    {message && <div role="alert" ref={summary} tabIndex={-1} className="error-summary">
      <p>{message}</p>
      {Object.entries(errors).some(([, value]) => !!value) && <ul>
        {Object.entries(errors).filter(([, value]) => !!value).map(([field, value]) => <li key={field}>
          <a href={`#register-${field}`} onClick={event => { event.preventDefault(); document.getElementById(field === 'accountType' ? 'register-member' : `register-${field}`)?.focus() }}>{value}</a>
        </li>)}
      </ul>}
    </div>}
    <form noValidate onSubmit={submit} aria-busy={busy}>
      <fieldset disabled={busy} className="registration-fields">
        <legend className="visually-hidden">Registrační údaje</legend>
        {input('firstName')}{input('lastName')}
        {input('email')}{input('password')}{input('passwordConfirmation')}
        <fieldset className="account-types" aria-describedby={errors.accountType ? 'register-accountType-error' : 'account-type-hint'} aria-invalid={!!errors.accountType}>
          <legend>Typ účtu <span aria-hidden="true">*</span></legend>
          <p className="field-hint" id="account-type-hint">Vyberte právě jeden typ účtu.</p>
          <div className="account-type-options">
            {([{ value: 'STUDENT', label: 'Člen', hint: 'Pracuje v týmech a může založit projekt.' },
              { value: 'TEACHER', label: 'Vyučující', hint: 'Projekty uvidí po přidělení přístupu.' }] as const).map(type =>
              <label className="account-type" key={type.value}>
                <input id={type.value === 'STUDENT' ? 'register-member' : 'register-teacher'} type="radio" name="accountType" required
                  value={type.value} checked={fields.accountType === type.value} onChange={() => update('accountType', type.value)} />
                <span><strong>{type.label}</strong><small>{type.hint}</small></span>
              </label>)}
          </div>
          {errorText('accountType')}
        </fieldset>
        <div className="registration-privacy">
          <label htmlFor="register-privacyAccepted">
            <input id="register-privacyAccepted" name="privacyAccepted" type="checkbox" required
              checked={fields.privacyAccepted} onChange={event => update('privacyAccepted', event.target.checked)}
              aria-invalid={!!errors.privacyAccepted}
              aria-describedby={errors.privacyAccepted ? 'register-privacyAccepted-error' : undefined} />
            <span>Seznámil/a jsem se se <a href={privacyPolicyUrl} target="_blank" rel="noopener noreferrer">zásadami ochrany osobních údajů</a>. <span aria-hidden="true">*</span></span>
          </label>
          <p className="field-hint">Zásady se otevřou v nové kartě. Vyplněný formulář zůstane zachovaný.</p>
          {errorText('privacyAccepted')}
        </div>
        <button className="registration-submit" type="submit" disabled={busy || remaining > 0}>
          {busy ? 'Vytváříme účet…' : 'Vytvořit účet'}
        </button>
      </fieldset>
      {remaining > 0 && <p className="field-hint">Další pokus bude možný za {remaining} s.</p>}
    </form>
    <p>Už máte účet? <a href="/login" onClick={event => { event.preventDefault(); onLogin() }}>Přihlaste se</a>.</p>
  </section>
}
