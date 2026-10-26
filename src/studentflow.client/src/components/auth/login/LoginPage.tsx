import { useLogin } from '../../../hooks/auth/useLogin'
import { devTestAccount } from '../../../services/auth/authApi'
import { FormField } from '../../common/form-field/FormField'
import { AuthCard } from '../card/AuthCard'
import type { LoginPageProps } from './LoginPageTypes'

/// <summary>
/// Zobrazuje obrazovku Přihlášení.
/// </summary>
export function LoginPage({ notice, onLoggedIn, onGoToRegister }: LoginPageProps) {
  const { values, errors, status, submitError, setValue, submit } = useLogin({ onLoggedIn })
  const submitting = status === 'submitting'

  return (
    <AuthCard titleId="login-title" title="Přihlášení" lead="Přihlaste se ke svým týmovým projektům.">
      {notice && <p className="auth-card__notice" role="status">{notice}</p>}
      {import.meta.env.DEV && (
        <p className="auth-card__notice">
          Testovací účet: <code>{devTestAccount.email}</code> / <code>{devTestAccount.password}</code>
        </p>
      )}
      <form onSubmit={submit} noValidate aria-busy={submitting}>
        <fieldset disabled={submitting} className="auth-card__fields">
          <FormField
            id="login-email"
            label="E-mail"
            type="email"
            autoComplete="email"
            placeholder="jmeno@example.cz"
            value={values.email}
            error={errors.email}
            onChange={(value) => setValue('email', value)}
          />
          <FormField
            id="login-password"
            label="Heslo"
            type="password"
            autoComplete="current-password"
            value={values.password}
            error={errors.password}
            onChange={(value) => setValue('password', value)}
          />
          <label className="auth-card__remember">
            <input
              type="checkbox"
              checked={values.rememberMe}
              onChange={(event) => setValue('rememberMe', event.target.checked)}
            />
            Zůstat přihlášen
          </label>
        </fieldset>

        {submitError && <p className="auth-card__error" role="alert">{submitError}</p>}

        <button type="submit" className="auth-card__submit" disabled={submitting}>
          {submitting ? 'Přihlašuji…' : 'Přihlásit se'}
        </button>
        <p className="auth-card__switch">
          Nemáte účet? <button type="button" onClick={onGoToRegister}>Zaregistrujte se</button>
        </p>
      </form>
    </AuthCard>
  )
}
