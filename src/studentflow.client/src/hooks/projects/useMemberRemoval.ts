import { useEffect, useRef, useState } from 'react'
import { useAuth } from '../../auth/AuthContext'
import type { ProjectMember } from '../../models/projects/ProjectMemberTypes'
import { CsrfTokenError } from '../../services/http/csrfRequest'
import { ProjectMembersFailure, removeProjectMember } from '../../services/projects/projectMembersApi'
import type { UseMemberRemovalResult } from './UseMemberRemovalTypes'

const connectionErrorMessage = 'Člena se nepodařilo odebrat. Zkontrolujte připojení a zkuste to znovu.'

/// <summary>
/// Odebírání členů s potvrzením v řádku: najednou je otevřené nejvýš jedno potvrzení a běží nejvýš jeden požadavek.
/// </summary>
/// <remarks>
/// Po každém výsledku kromě 401 nechá seznam znovu načíst: i neúspěch může znamenat, že se tým mezitím změnil
/// (člena odebral někdo jiný, uživatel přestal být vedoucím, nebo spojení spadlo až po odebrání).
/// </remarks>
export function useMemberRemoval(projectId: number, onChanged: () => void): UseMemberRemovalResult {
  const { refresh } = useAuth()
  const [confirmingUserId, setConfirmingUserId] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [success, setSuccess] = useState<string | null>(null)
  const request = useRef<AbortController | null>(null)

  useEffect(() => () => request.current?.abort(), [])

  /// <summary>
  /// Otevře potvrzení u člena a smaže zprávy z předchozího pokusu.
  /// </summary>
  function ask(userId: string) {
    if (busy) return
    setConfirmingUserId(userId)
    setError(null)
    setSuccess(null)
  }

  /// <summary>
  /// Zavře potvrzení; během běžícího požadavku nic nedělá.
  /// </summary>
  function cancel() {
    if (!busy) setConfirmingUserId(null)
  }

  /// <summary>
  /// Odebere člena a podle výsledku ukáže zprávu a nechá seznam znovu načíst.
  /// </summary>
  async function confirm(member: ProjectMember) {
    if (busy) return
    const controller = new AbortController()
    request.current = controller
    setBusy(true)
    try {
      await removeProjectMember(projectId, member.userId, controller.signal)
      if (controller.signal.aborted) return
      setSuccess(`${member.displayName} už není členem týmu.`)
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
      onChanged()
    } finally {
      if (!controller.signal.aborted) {
        setBusy(false)
        setConfirmingUserId(null)
      }
    }
  }

  return { confirmingUserId, busy, error, success, ask, cancel, confirm }
}
