import { useEffect, useState } from 'react'
import { useAuth } from '../auth/AuthContext'
import { isAuthUser, isRecord } from '../auth/types'
import type { AuthUser } from '../auth/types'

type Result = { status: 'loading' } | { status: 'error' } | { status: 'forbidden' }
  | { status: 'loaded'; user: AuthUser }

// Vzor pro předání dashboardu; App ho nemountuje, dokud vývojář nezapojí i vzorový controller.
export function AccountSummaryPage() {
  const { state, refresh } = useAuth()
  if (state.status !== 'authenticated') return null
  return <AccountSummaryContent key={state.identity.user.id} refresh={refresh} />
}

function AccountSummaryContent({ refresh }: { refresh: () => Promise<void> }) {
  const [result, setResult] = useState<Result>({ status: 'loading' })
  useEffect(() => {
    const controller = new AbortController()
    let active = true
    async function load() {
      try {
        const response = await fetch('/api/account/summary', {
          credentials: 'same-origin', cache: 'no-store', signal: controller.signal,
        })
        if (!active) return
        if (response.status === 401) {
          await refresh()
          if (active) setResult({ status: 'error' })
          return
        }
        if (response.status === 403) { if (active) setResult({ status: 'forbidden' }); return }
        if (response.status !== 200) throw new Error('Account summary could not be loaded.')
        const body: unknown = await response.json()
        if (!isRecord(body) || !isAuthUser(body.user)) throw new Error('Invalid account summary.')
        if (active) setResult({ status: 'loaded', user: body.user })
      } catch {
        if (active && !controller.signal.aborted) setResult({ status: 'error' })
      }
    }
    void load()
    return () => { active = false; controller.abort() }
  }, [refresh])

  if (result.status === 'loading') return <p role="status">Načítáme údaje účtu…</p>
  if (result.status === 'error') return <p role="alert">Údaje se nepodařilo načíst. Zkuste obnovit stránku.</p>
  if (result.status === 'forbidden') return <p role="alert">K tomuto přehledu nemáte přístup.</p>
  return <section aria-labelledby="account-summary-title">
    <h2 id="account-summary-title">Můj účet</h2>
    <p>{result.user.displayName}</p>
    <p>{result.user.email}</p>
    <p>{result.user.accountType === 'STUDENT' ? 'Člen' : 'Vyučující'}</p>
  </section>
}
