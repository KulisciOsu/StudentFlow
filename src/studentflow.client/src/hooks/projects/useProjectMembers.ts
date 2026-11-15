import { useCallback, useEffect, useState } from 'react'
import { useAuth } from '../../auth/AuthContext'
import { getProjectMembers, ProjectMembersFailure } from '../../services/projects/projectMembersApi'
import type { ProjectMembersState, UseProjectMembersResult } from './UseProjectMembersTypes'

const connectionErrorMessage = 'Členy projektu se nepodařilo načíst. Zkontrolujte připojení a zkuste to znovu.'

/// <summary>
/// Načte název týmu a členy projektu; po vypršení přihlášení (401) nechá aplikaci znovu ověřit session.
/// </summary>
/// <remarks>Pro jiný projekt komponentu vykreslete s jiným key, aby začala znovu ve stavu načítání.</remarks>
export function useProjectMembers(projectId: number): UseProjectMembersResult {
  const { refresh } = useAuth()
  const [state, setState] = useState<ProjectMembersState>({ status: 'loading' })
  const [version, setVersion] = useState(0)

  useEffect(() => {
    const controller = new AbortController()
    let active = true

    /// <summary>
    /// Načte seznam a promítne výsledek do stavu, pokud komponenta mezitím nezanikla.
    /// </summary>
    async function load() {
      try {
        const data = await getProjectMembers(projectId, controller.signal)
        if (active) setState({ status: 'loaded', data })
      } catch (error) {
        if (!active || controller.signal.aborted) return
        if (error instanceof ProjectMembersFailure && error.status === 401) {
          await refresh()
          return
        }
        setState(error instanceof ProjectMembersFailure
          ? { status: 'error', message: error.message, notFound: error.status === 404 }
          : { status: 'error', message: connectionErrorMessage, notFound: false })
      }
    }

    void load()
    return () => {
      active = false
      controller.abort()
    }
  }, [projectId, version, refresh])

  const reload = useCallback(() => setVersion(current => current + 1), [])
  return { state, reload }
}
