import { useEffect, useRef, useState } from 'react'
import type { FormEvent } from 'react'
import type { RegisterErrors, RegisterFormValues } from '../../models/auth/AuthTypes'
import { register } from '../../services/auth/authApi'
import { validateRegister } from '../../utils/auth/authValidation'
import type { RegisterStatus, UseRegisterOptions, UseRegisterResult } from './UseRegisterTypes'

/// <summary>
/// Spravuje formulář registrace a jeho odeslání do API.
/// </summary>
export function useRegister({ onRegistered }: UseRegisterOptions): UseRegisterResult {
  const [values, setValues] = useState<RegisterFormValues>({
    name: '', email: '', password: '', passwordConfirm: '', accountType: 'student',
  })
  const [errors, setErrors] = useState<RegisterErrors>({})
  const [status, setStatus] = useState<RegisterStatus>('idle')
  const [submitError, setSubmitError] = useState<string | null>(null)
  const controllerRef = useRef<AbortController | null>(null)

  useEffect(() => () => controllerRef.current?.abort(), [])

  /// <summary>
  /// Změní hodnotu pole a smaže jeho chybu.
  /// </summary>
  function setValue<K extends keyof RegisterFormValues>(field: K, value: RegisterFormValues[K]) {
    setValues((current) => ({ ...current, [field]: value }))
    setErrors((current) => ({ ...current, [field]: undefined }))
  }

  /// <summary>
  /// Ověří a odešle formulář.
  /// </summary>
  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (status === 'submitting') return

    const validationErrors = validateRegister(values)
    setErrors(validationErrors)
    setSubmitError(null)
    if (Object.keys(validationErrors).length > 0) return

    const controller = new AbortController()
    controllerRef.current = controller
    setStatus('submitting')

    try {
      await register({
        name: values.name.trim(),
        email: values.email.trim(),
        password: values.password,
        accountType: values.accountType,
      }, controller.signal)
      if (controller.signal.aborted) return
      onRegistered()
    } catch {
      if (controller.signal.aborted) return
      setSubmitError('Účet se nepodařilo vytvořit. Zkuste to prosím znovu.')
      setStatus('error')
    }
  }

  return { values, errors, status, submitError, setValue, submit }
}
