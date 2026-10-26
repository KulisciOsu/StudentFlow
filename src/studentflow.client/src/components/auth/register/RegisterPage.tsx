import { useRegister } from '../../../hooks/auth/useRegister'
import type { AccountType } from '../../../models/auth/AuthTypes'
import { minPasswordLength } from '../../../utils/auth/authValidation'
import { FormField } from '../../common/form-field/FormField'
import { AuthCard } from '../card/AuthCard'
import type { RegisterPageProps } from './RegisterPageTypes'
import './RegisterPage.scss'

const accountTypes: { value: AccountType, title: string, description: string }[] = [
  {
    value: 'student',
    title: 'Student',
    description: 'Pracuje v týmech. Založením projektu se stane jeho vedoucím.',
  },
  {
    value: 'teacher',
    title: 'Vyučující',
    description: 'Projekty uvidí, až ho do nich přizve vedoucí týmu.',
  },
]

/// <summary>
/// Zobrazuje obrazovku Registrace.
/// </summary>
export function RegisterPage({ onRegistered, onGoToLogin }: RegisterPageProps) {
  const { values, errors, status, submitError, setValue, submit } = useRegister({ onRegistered })
  const submitting = status === 'submitting'

  return (
    <AuthCard
      titleId="register-title"
      title="Registrace"
      lead="Vytvořte si účet. Do projektu vás pak přizve vedoucí týmu."
    >
      <form onSubmit={submit} noValidate aria-busy={submitting}>
        <fieldset disabled={submitting} className="auth-card__fields">
          <FormField
            id="register-name"
            label="Jméno a příjmení"
            autoComplete="name"
            placeholder="Jan Novák"
            value={values.name}
            error={errors.name}
            onChange={(value) => setValue('name', value)}
          />
          <FormField
            id="register-email"
            label="E-mail"
            type="email"
            autoComplete="email"
            placeholder="jmeno@example.cz"
            value={values.email}
            error={errors.email}
            onChange={(value) => setValue('email', value)}
          />
          <FormField
            id="register-password"
            label="Heslo"
            type="password"
            autoComplete="new-password"
            placeholder={`Alespoň ${minPasswordLength} znaků`}
            value={values.password}
            error={errors.password}
            onChange={(value) => setValue('password', value)}
          />
          <FormField
            id="register-password-confirm"
            label="Heslo znovu"
            type="password"
            autoComplete="new-password"
            value={values.passwordConfirm}
            error={errors.passwordConfirm}
            onChange={(value) => setValue('passwordConfirm', value)}
          />
        </fieldset>

        <fieldset disabled={submitting} className="register-account-type">
          <legend>Typ účtu</legend>
          {accountTypes.map(({ value, title, description }) => (
            <label
              key={value}
              className={`register-account-type__option${values.accountType === value ? ' register-account-type__option--selected' : ''}`}
            >
              <input
                type="radio"
                name="accountType"
                value={value}
                checked={values.accountType === value}
                onChange={() => setValue('accountType', value)}
              />
              <span>
                <strong>{title}</strong>
                <span>{description}</span>
              </span>
            </label>
          ))}
        </fieldset>

        {submitError && <p className="auth-card__error" role="alert">{submitError}</p>}

        <button type="submit" className="auth-card__submit" disabled={submitting}>
          {submitting ? 'Vytvářím účet…' : 'Vytvořit účet'}
        </button>
        <p className="auth-card__switch">
          Už máte účet? <button type="button" onClick={onGoToLogin}>Přihlaste se</button>
        </p>
      </form>
    </AuthCard>
  )
}
