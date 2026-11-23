import { spawnSync } from 'node:child_process'
import { chmodSync, existsSync, mkdirSync } from 'node:fs'
import { join } from 'node:path'

/**
 * Zajistí vývojový HTTPS certifikát ASP.NET Core exportovaný do .certs/ v kořeni repozitáře.
 * Sdílí ho API (scripts/dev.mjs), Vite i Playwright. Chybějící soubory vytvoří přes
 * `dotnet dev-certs https`; existující ponechá, pokud není požadován nový export.
 * Důvěru certifikátu v systému nemění.
 * @param {string} root Kořen repozitáře.
 * @param {{ force?: boolean }} [options] force vynutí nový export i při existujících souborech.
 * @returns {{ certPath: string, keyPath: string, created: boolean }}
 */
export function ensureDevCertificate(root, { force = false } = {}) {
  const directory = join(root, '.certs')
  const certPath = join(directory, 'localhost.pem')
  const keyPath = join(directory, 'localhost.key')

  if (!force && existsSync(certPath) && existsSync(keyPath)) {
    return { certPath, keyPath, created: false }
  }

  mkdirSync(directory, { recursive: true, mode: 0o700 })
  const result = spawnSync('dotnet', [
    'dev-certs', 'https', '--export-path', certPath, '--format', 'PEM', '--no-password',
  ], { cwd: root, stdio: 'inherit' })

  if (result.error || result.status !== 0 || !existsSync(certPath) || !existsSync(keyPath)) {
    throw new Error('Export vývojového HTTPS certifikátu selhal. Ověřte instalaci .NET 10 SDK a příkaz dotnet dev-certs https.')
  }

  if (process.platform !== 'win32') chmodSync(keyPath, 0o600)
  return { certPath, keyPath, created: true }
}
