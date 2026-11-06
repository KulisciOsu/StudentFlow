import { useSyncExternalStore } from 'react'
import { kanbanConfig } from '../components/kanban/kanbanConfig'

/// <summary>
/// Přihlásí odběr změny šířky okna.
/// </summary>
function subscribe(onChange: () => void) {
  const query = window.matchMedia(kanbanConfig.mobileQuery)
  query.addEventListener('change', onChange)
  return () => query.removeEventListener('change', onChange)
}

/// <summary>
/// Určí, zda se nástěnka zobrazuje na mobilu.
/// </summary>
export function useIsMobile(): boolean {
  return useSyncExternalStore(subscribe, () => window.matchMedia(kanbanConfig.mobileQuery).matches)
}
