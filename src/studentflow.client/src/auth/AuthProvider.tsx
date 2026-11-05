import { useCallback, useEffect, useRef, useState } from 'react'
import type { ReactNode } from 'react'
import { AuthContext } from './AuthContext'
import type { AuthState } from './AuthContext'
import { fetchIdentity, loginAccount, logoutAccount } from './session'
import type { LoginFields } from './session'
import type { Identity } from './types'

/** Obnovuje serverovou identitu a sdílí změny přihlášení mezi stránkami i kartami prohlížeče. */
export function AuthProvider({ children }: { children: ReactNode }) {
  const [state, setState] = useState<AuthState>({ status: 'loading' })
  const current = useRef<AuthState>(state)
  const epoch = useRef(0)
  const request = useRef<AbortController | null>(null)
  const hadIdentity = useRef(false)
  const channel = useRef<BroadcastChannel | null>(null)
  const commit = useCallback((value: AuthState) => { current.current = value; setState(value) }, [])
  /** Zruší předchozí požadavek; generace brání přepsání nového stavu opožděnou odpovědí. */
  const begin = useCallback(() => {
    request.current?.abort()
    const controller = new AbortController()
    request.current = controller
    return { controller, generation: ++epoch.current }
  }, [])
  const receive = useCallback((identity: Identity | null, controller: AbortController, generation: number) => {
    if (controller.signal.aborted || generation !== epoch.current) return
    if (identity) { hadIdentity.current = true; commit({ status: 'authenticated', identity }) }
    else { commit({ status: 'anonymous', notice: hadIdentity.current ? 'expired' : undefined }); hadIdentity.current = false }
  }, [commit])
  const fail = useCallback((controller: AbortController, generation: number) => {
    if (!controller.signal.aborted && generation === epoch.current) commit({ status: 'error' })
  }, [commit])
  const refresh = useCallback(async () => {
    if (current.current.status === 'loggingOut' || current.current.status === 'logoutError') return
    const { controller, generation } = begin()
    commit({ status: 'loading' })
    try { receive(await fetchIdentity(controller.signal), controller, generation) }
    catch { fail(controller, generation) }
  }, [begin, commit, receive, fail])
  const cancel = useCallback(() => { request.current?.abort(); epoch.current++ }, [])
  const signIn = useCallback(async (fields: LoginFields, signal?: AbortSignal) => {
    const { controller, generation } = begin()
    const combined = signal ? AbortSignal.any([controller.signal, signal]) : controller.signal
    const identity = await loginAccount(fields, combined)
    if (combined.aborted || generation !== epoch.current) throw new DOMException('Aborted', 'AbortError')
    hadIdentity.current = true
    commit({ status: 'authenticated', identity })
    channel.current?.postMessage('changed')
  }, [begin, commit])
  const signOut = useCallback(async () => {
    if (current.current.status === 'loggingOut') return
    const { controller, generation } = begin()
    hadIdentity.current = false
    commit({ status: 'loggingOut' })
    try {
      await logoutAccount(controller.signal)
      if (!controller.signal.aborted && generation === epoch.current) {
        commit({ status: 'anonymous', notice: 'loggedOut' })
        channel.current?.postMessage('changed')
      }
    } catch {
      if (!controller.signal.aborted && generation === epoch.current) commit({ status: 'logoutError' })
    }
  }, [begin, commit])

  useEffect(() => {
    const { controller, generation } = begin()
    void fetchIdentity(controller.signal).then(identity => receive(identity, controller, generation), () => fail(controller, generation))
    const visibility = () => { if (document.visibilityState === 'visible') void refresh() }
    const restored = (event: PageTransitionEvent) => { if (event.persisted) void refresh() }
    document.addEventListener('visibilitychange', visibility)
    window.addEventListener('pageshow', restored)
    const broadcast = new BroadcastChannel('studentflow-auth')
    channel.current = broadcast
    broadcast.onmessage = event => { if (event.data === 'changed') void refresh() }
    return () => {
      cancel()
      document.removeEventListener('visibilitychange', visibility)
      window.removeEventListener('pageshow', restored)
      broadcast.close(); channel.current = null
    }
  }, [begin, receive, fail, refresh, cancel])
  useEffect(() => {
    if (state.status !== 'authenticated') return
    const delay = Math.max(60_000, Date.parse(state.identity.session.expiresAt) - Date.now())
    const timer = window.setTimeout(() => { void refresh() }, Math.min(delay, 2_147_483_647))
    return () => window.clearTimeout(timer)
  }, [state, refresh])

  return <AuthContext.Provider value={{ state, refresh, signIn, signOut }}>{children}</AuthContext.Provider>
}
