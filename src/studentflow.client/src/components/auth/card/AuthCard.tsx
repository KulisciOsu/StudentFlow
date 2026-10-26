import type { AuthCardProps } from './AuthCardTypes'
import './AuthCard.scss'

/// <summary>
/// Zobrazuje kartu s nadpisem pro Přihlášení a Registraci.
/// </summary>
export function AuthCard({ titleId, title, lead, children }: AuthCardProps) {
  return (
    <section className="auth-card" aria-labelledby={titleId}>
      <h2 id={titleId}>{title}</h2>
      <p className="auth-card__lead">{lead}</p>
      {children}
    </section>
  )
}
