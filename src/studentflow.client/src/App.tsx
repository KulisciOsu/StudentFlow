import { useEffect, useState } from 'react'
import type { ConnectionStatus } from './AppTypes'
import './App.css'

const connectionMessages: Record<ConnectionStatus, string> = {
  checking: 'Ověřuji připojení k API…',
  connected: 'API je připojené.',
  unavailable: 'API není dostupné. Zkontrolujte, zda je backend spuštěný.',
}

/// <summary>
/// Zobrazuje úvodní stránku StudentFlow a stav připojení k API.
/// </summary>
function App() {
  const [connectionStatus, setConnectionStatus] =
    useState<ConnectionStatus>('checking')

  useEffect(() => {
    const controller = new AbortController()
    let disposed = false
    const timeoutId = window.setTimeout(() => controller.abort(), 5000)

    /// <summary>
    /// Ověří odpověď health endpointu a aktualizuje stav připojení.
    /// </summary>
    async function checkConnection() {
      try {
        const response = await fetch('/api/health', {
          signal: controller.signal,
        })

        if (!response.ok) {
          throw new Error('Health request failed')
        }

        const health: unknown = await response.json()

        if (
          typeof health !== 'object' ||
          health === null ||
          !('status' in health) ||
          health.status !== 'OK'
        ) {
          throw new Error('Unexpected health response')
        }

        if (!disposed) {
          setConnectionStatus('connected')
        }
      } catch {
        if (!disposed) {
          setConnectionStatus('unavailable')
        }
      } finally {
        window.clearTimeout(timeoutId)
      }
    }

    void checkConnection()

    return () => {
      disposed = true
      window.clearTimeout(timeoutId)
      controller.abort()
    }
  }, [])

  return (
    <main className="welcome">
      <h1>StudentFlow</h1>
      <p className="welcome-description">
        Správa studentských týmových projektů na jednom místě.
      </p>
      <p
        className={`connection-status connection-status--${connectionStatus}`}
        role="status"
      >
        <span className="connection-indicator" aria-hidden="true" />
        {connectionMessages[connectionStatus]}
      </p>
    </main>
  )
}

export default App
