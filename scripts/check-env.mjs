import { spawnSync } from 'node:child_process'

const args = process.argv.slice(2)
if (args.includes('--help')) {
  console.log('Použití: node scripts/check-env.mjs [--external-db]')
  console.log('--external-db: použít vyhrazenou PostgreSQL 17 přes ConnectionStrings__DefaultConnection místo Dockeru.')
  process.exit(0)
}
if (args.some(arg => arg !== '--external-db')) {
  console.error('Neznámá volba. Použijte --help.')
  process.exit(2)
}

let failed = false
function check(label, ok, detail) {
  console.log(`${ok ? 'OK' : 'CHYBA'}: ${label} — ${detail}`)
  failed ||= !ok
}
function execute(command, arguments_) {
  return spawnSync(command, arguments_, { encoding: 'utf8', timeout: 10_000, windowsHide: true })
}
function run(command, arguments_) {
  const result = execute(command, arguments_)
  return result.status === 0 ? result.stdout.trim() || null : null
}

console.log('StudentFlow: kontrola nástrojů před spuštěním')
const [major, minor] = process.versions.node.split('.').map(Number)
check('Node.js', major === 22 && minor >= 13, `${process.versions.node}; požadováno 22.x od 22.13`)
const npm = process.platform === 'win32'
  ? run('cmd.exe', ['/d', '/s', '/c', 'npm --version'])
  : run('npm', ['--version'])
check('npm', !!npm, npm ?? 'nenalezeno; nainstalujte spolu s Node.js 22')
const sdk = run('dotnet', ['--version'])
check('.NET SDK', !!sdk && /^10\./u.test(sdk), sdk ?? 'nenalezeno .NET 10 SDK; samotný runtime nestačí')

if (args.includes('--external-db')) {
  check('Externí databáze', !!process.env.ConnectionStrings__DefaultConnection,
    process.env.ConnectionStrings__DefaultConnection
      ? 'connection string je nastaven; dostupnost a migrace ověřte registrací nebo integračními testy'
      : 'nastavte ConnectionStrings__DefaultConnection pro PostgreSQL 17')
} else {
  const compose = run('docker', ['compose', 'version', '--short'])
  check('Docker Compose', !!compose && /^v?(?:[2-9]|[1-9]\d+)\./u.test(compose), compose ?? 'nenalezeno; vyžadováno Compose 2 nebo novější')
  const result = compose ? execute('docker', ['info', '--format', '{{.ServerVersion}}']) : null
  const daemon = result?.status === 0 ? result.stdout.trim() || null : null
  const accessDenied = /permission denied|access is denied|access denied/iu.test(result?.stderr ?? '')
  const hint = accessDenied
    ? 'přístup k Docker enginu byl odmítnut; Linux/WSL: ověřte skupinu docker a po změně spusťte newgrp docker; postup v documents/DEVELOPMENT.md'
    : 'není dostupný; spusťte Docker Desktop/engine'
  check('Docker engine', !!daemon, daemon ?? hint)
}

if (failed) console.error('Nejprve opravte uvedené požadavky. Postup: documents/DEVELOPMENT.md.')
else console.log('Nástroje jsou připravené. Pokračujte instalací závislostí, setupem a migracemi podle documents/DEVELOPMENT.md.')
process.exitCode = failed ? 1 : 0
