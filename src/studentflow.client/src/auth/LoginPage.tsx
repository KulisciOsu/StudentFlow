import { useEffect, useRef, useState } from 'react'
import type { FormEvent } from 'react'
import { useAuth } from './AuthContext'
import { AuthFailure } from './client'
import { validateLogin } from './session'
import type { LoginErrors } from './session'

export function LoginPage({ initialEmail = '', notice, onRegister }: { initialEmail?: string; notice?: string; onRegister: () => void }) {
  const { signIn, refresh } = useAuth()
  const [email, setEmail] = useState(initialEmail)
  const [password, setPassword] = useState('')
  const [rememberMe, setRememberMe] = useState(false)
  const [visible, setVisible] = useState(false)
  const [errors, setErrors] = useState<LoginErrors>({})
  const [message, setMessage] = useState('')
  const [uncertain, setUncertain] = useState(false)
  const [busy, setBusy] = useState(false)
  const [revision, setRevision] = useState(0)
  const [retryUntil, setRetryUntil] = useState(0)
  const [remaining, setRemaining] = useState(0)
  const summary = useRef<HTMLDivElement>(null)
  const request = useRef<AbortController | null>(null)
  const inFlight = useRef(false)
  useEffect(() => () => request.current?.abort(), [])
  useEffect(() => { if (revision) summary.current?.focus() }, [revision])
  useEffect(() => {
    if (!retryUntil) return
    const timer = window.setInterval(() => setRemaining(Math.max(0, Math.ceil((retryUntil - Date.now()) / 1000))), 1000)
    return () => window.clearInterval(timer)
  }, [retryUntil])
  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (inFlight.current || Date.now() < retryUntil) return
    const fields = { email, password, rememberMe }
    const validation = validateLogin(fields)
    setErrors(validation)
    if (Object.keys(validation).length) { setMessage('Opravte označená pole.'); setRevision(value => value + 1); return }
    const controller = new AbortController()
    request.current = controller; inFlight.current = true
    setBusy(true); setMessage(''); setUncertain(false)
    try { await signIn(fields, controller.signal) }
    catch (error) {
      if (!controller.signal.aborted) {
        setPassword('')
        setErrors(error instanceof AuthFailure ? error.errors : {})
        setMessage(error instanceof AuthFailure ? error.message : 'Přihlášení se nepodařilo potvrdit. Zkontrolujte připojení.')
        setUncertain(!(error instanceof AuthFailure) || error.status === 0 || error.status >= 500)
        if (error instanceof AuthFailure && error.retryAfter) { setRetryUntil(Date.now() + error.retryAfter * 1000); setRemaining(error.retryAfter) }
        setRevision(value => value + 1)
      }
    } finally { inFlight.current = false; if (!controller.signal.aborted) setBusy(false) }
  }
  return <section aria-labelledby="login-title">
    <h2 id="login-title" tabIndex={-1}>Přihlášení</h2>
    {notice && <p role="status">{notice}</p>}
    <p>Přihlaste se ke svému účtu StudentFlow.</p>
    {message && <div className="error-summary" role="alert" tabIndex={-1} ref={summary}>
      <p>{message}</p>
      {Object.entries(errors).some(([, value]) => value) && <ul>{Object.entries(errors).filter(([, value]) => value).map(([field, value]) =>
        <li key={field}><a href={`#login-${field}`} onClick={event => { event.preventDefault(); document.getElementById(`login-${field}`)?.focus() }}>{value}</a></li>)}</ul>}
      {uncertain && <button type="button" onClick={() => void refresh()}>Ověřit přihlášení</button>}
    </div>}
    <form noValidate onSubmit={submit} aria-busy={busy}>
      <fieldset className="registration-fields" disabled={busy}>
        <legend className="visually-hidden">Přihlašovací údaje</legend>
        <div className="registration-field">
          <label htmlFor="login-email">E-mail</label>
          <input id="login-email" name="email" type="email" autoComplete="username" autoCapitalize="none" spellCheck={false} required
            value={email} onChange={event => { setEmail(event.target.value); setErrors(value => ({ ...value, email: undefined })) }}
            aria-invalid={!!errors.email} aria-describedby={errors.email ? 'login-email-error' : undefined} />
          {errors.email && <p className="field-error" id="login-email-error">{errors.email}</p>}
        </div>
        <div className="registration-field">
          <label htmlFor="login-password">Heslo</label>
          <div className="password-input">
            <input id="login-password" name="password" type={visible ? 'text' : 'password'} autoComplete="current-password" required
              value={password} onChange={event => { setPassword(event.target.value); setErrors(value => ({ ...value, password: undefined })) }}
              aria-invalid={!!errors.password} aria-describedby={errors.password ? 'login-password-error' : undefined} />
            <button type="button" aria-pressed={visible} aria-label={`${visible ? 'Skrýt' : 'Zobrazit'} heslo`} onClick={() => setVisible(value => !value)}>{visible ? 'Skrýt' : 'Zobrazit'}</button>
          </div>
          {errors.password && <p className="field-error" id="login-password-error">{errors.password}</p>}
        </div>
        <label className="remember-me"><input type="checkbox" checked={rememberMe} onChange={event => setRememberMe(event.target.checked)} />
          <span>Zůstat přihlášen<small>Přihlášení až na 30 dní na tomto zařízení.</small></span>
        </label>
        <button className="registration-submit" disabled={busy || remaining > 0} type="submit">{busy ? 'Přihlašujeme…' : 'Přihlásit se'}</button>
      </fieldset>
      {remaining > 0 && <p className="field-hint">Další pokus bude možný za {remaining} s.</p>}
    </form>
    <p>Nemáte účet? <a href="/register" onClick={event => { event.preventDefault(); onRegister() }}>Zaregistrujte se</a>.</p>
  </section>
}
