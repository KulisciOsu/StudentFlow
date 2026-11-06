import type { MoveErrorToastProps } from './MoveErrorToastTypes'
import './MoveErrorToast.scss'

/// <summary>
/// Oznamuje, že se přesun nepodařilo uložit a karta se vrátila zpět.
/// </summary>
export function MoveErrorToast({ taskTitle, columnTitle, onRetry, onDismiss }: MoveErrorToastProps) {
  return (
    <div className="move-error-toast" role="alert">
      <p>
        Přesun úkolu „{taskTitle}“ se nepodařilo uložit.
        Karta se vrátila do sloupce {columnTitle}.
      </p>
      <div className="move-error-toast-actions">
        <button type="button" onClick={onRetry}>Zkusit znovu</button>
        <button type="button" onClick={onDismiss} aria-label="Zavřít">✕</button>
      </div>
    </div>
  )
}
