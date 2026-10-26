import { useEffect, useRef, useState } from 'react'
import type { FormEvent } from 'react'
import type { LoginErrors, LoginFormValues } from '../../models/auth/AuthTypes'
import { InvalidCredentialsError, login } from '../../services/auth/authApi'
import { validateLogin } from '../../utils/auth/authValidation'
import type { LoginStatus, UseLoginOptions, UseLoginResult } from './UseLoginTypes'

/// <summary>
/// Spravuje formulář přihlášení a jeho odeslání do API.
/// </summary>
export function useLogin({ onLoggedIn }: UseLoginOptions): UseLoginResult {
  const [values, setValues] = useState<LoginFormValues>({ email: '', password: '', rememberMe: false })
  const [errors, setErrors] = useState<LoginErrors>({})
  const [status, setStatus] = useState<LoginStatus>('idle')
  const [submitError, setSubmitError] = useState<string | null>(null)
  const controllerRef = useRef<AbortController | null>(null)

  useEffect(() => () => controllerRef.current?.abort(), [])

  /// <summary>
  /// Změní hodnotu pole a smaže jeho chybu.
  /// </summary>
  function setValue<K extends keyof LoginFormValues>(field: K, value: LoginFormValues[K]) {
    setValues((current) => ({ ...current, [field]: value }))
    setErrors((current) => ({ ...current, [field]: undefined }))
  }

  /// <summary>
  /// Ověří a odešle formulář.
  /// </summary>
  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (status === 'submitting') return

    const validationErrors = validateLogin(values)
    setErrors(validationErrors)
    setSubmitError(null)
    if (Object.keys(validationErrors).length > 0) return

    const controller = new AbortController()
    controllerRef.current = controller
    setStatus('submitting')

    try {
      const user = await login(values, controller.signal)
      if (controller.signal.aborted) return
      onLoggedIn(user)
    } catch (error) {
      if (controller.signal.aborted) return

      // Hláška nesmí prozradit, zda účet existuje.
      setSubmitError(error instanceof InvalidCredentialsError
        ? error.message
        : 'Přihlášení se nezdařilo. Zkuste to prosím znovu.')
      setStatus('error')
    }
  }

  return { values, errors, status, submitError, setValue, submit }
}
