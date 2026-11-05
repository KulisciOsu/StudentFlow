import { spawn, spawnSync } from 'node:child_process'
import { randomBytes } from 'node:crypto'
import { chmodSync, existsSync, mkdirSync, readFileSync, writeFileSync } from 'node:fs'
import { get } from 'node:https'
import { dirname, join, resolve } from 'node:path'
import { fileURLToPath } from 'node:url'
import * as nodeUtil from 'node:util'
import { setTimeout as delay } from 'node:timers/promises'

const root = resolve(dirname(fileURLToPath(import.meta.url)), '..')
const client = join(root, 'src/studentflow.client')
const envPath = join(root, '.env')
const certPath = join(root, '.certs/localhost.pem')
const keyPath = join(root, '.certs/localhost.key')
const command = process.argv[2] ?? 'start'

function fail(message) {
  console.error(message)
  process.exit(1)
}

const [nodeMajor, nodeMinor] = process.versions.node.split('.').map(Number)
if (nodeMajor !== 22 || nodeMinor < 13) {
  fail('Použijte Node.js 22 (alespoň 22.13) podle .nvmrc.')
}

if (command === 'setup') {
  if (!existsSync(envPath)) {
    const template = readFileSync(join(root, '.env.example'), 'utf8')
    writeFileSync(envPath, template.replace('POSTGRES_PASSWORD=', `POSTGRES_PASSWORD=${randomBytes(24).toString('hex')}`), { mode: 0o600, flag: 'wx' })
    console.log('Vytvořen lokální .env s vygenerovaným heslem databáze.')
  }
  mkdirSync(dirname(certPath), { recursive: true, mode: 0o700 })
  const result = spawnSync('dotnet', [
    'dev-certs', 'https', '--export-path', certPath, '--format', 'PEM', '--no-password',
  ], { cwd: root, stdio: 'inherit' })
  if (result.error || result.status !== 0) {
    fail('Export certifikátu selhal. Ověřte instalaci .NET 10 SDK a zkuste setup znovu.')
  }
  if (process.platform !== 'win32') chmodSync(keyPath, 0o600)
  console.log('HTTPS certifikát připraven v .certs/. Důvěru lze nastavit pomocí dotnet dev-certs https --trust.')
  process.exit(0)
}

if (!['start', 'api'].includes(command)) fail('Použití: node scripts/dev.mjs [setup|start|api]')
if (!existsSync(envPath) || !existsSync(certPath) || !existsSync(keyPath)) {
  fail('Nejdříve z kořene projektu spusťte node scripts/dev.mjs setup.')
}

const env = { ...nodeUtil.parseEnv(readFileSync(envPath, 'utf8')), ...process.env }
if (!env.POSTGRES_PASSWORD && !env.ConnectionStrings__DefaultConnection) {
  fail('Nastavte POSTGRES_PASSWORD v .env nebo ConnectionStrings__DefaultConnection v prostředí.')
}
// Npgsql dovoluje dvojité uvozovky v hodnotě escapovat zdvojením.
const quotedPassword = (env.POSTGRES_PASSWORD ?? '').replaceAll('"', '""')
env.ConnectionStrings__DefaultConnection ??= `Host=127.0.0.1;Port=5433;Database=studentflow;Username=studentflow;Password="${quotedPassword}"`
env.ASPNETCORE_ENVIRONMENT = 'Development'
env.ASPNETCORE_Kestrel__Certificates__Default__Path = certPath
env.ASPNETCORE_Kestrel__Certificates__Default__KeyPath = keyPath

const children = []
let stopping = false

function stop(exitCode) {
  if (stopping) return
  stopping = true
  process.exitCode = exitCode
  for (const child of children) child.kill('SIGTERM')
  const deadline = setTimeout(() => {
    for (const child of children) child.kill('SIGKILL')
  }, 5000)
  deadline.unref()
}

function launch(executable, args, cwd) {
  const child = spawn(executable, args, { cwd, env, stdio: 'inherit' })
  children.push(child)
  child.on('error', (error) => {
    console.error(`Spuštění ${executable} selhalo: ${error.message}`)
    stop(1)
  })
  child.on('exit', (code) => {
    if (!stopping) stop(code === 0 ? 1 : (code ?? 1))
  })
}

async function waitForApi() {
  const deadline = Date.now() + 90_000
  while (!stopping && Date.now() < deadline) {
    const ready = await new Promise((resolveReady) => {
      // Vlastní lokální vývojový certifikát; nastavení se nepřenáší jinam.
      const request = get('https://localhost:7082/api/health', {
        rejectUnauthorized: false,
        timeout: 2000,
      }, (response) => {
        response.resume()
        resolveReady(response.statusCode === 200)
      })
      request.on('timeout', () => request.destroy())
      request.on('error', () => resolveReady(false))
    })
    if (ready && !stopping) return true
    await delay(500)
  }
  return false
}

process.on('SIGINT', () => stop(0))
process.on('SIGTERM', () => stop(0))

launch('dotnet', [
  'run', '--project', 'src/StudentFlow.Api/StudentFlow.Api.csproj',
  '--no-launch-profile', '--urls', 'https://localhost:7082',
], root)

if (command === 'start') {
  const vite = join(client, 'node_modules/vite/bin/vite.js')
  if (!existsSync(vite)) {
    console.error('Nejdříve spusťte npm --prefix src/studentflow.client ci.')
    stop(1)
  } else if (await waitForApi()) {
    launch(process.execPath, [vite], client)
    console.log('StudentFlow: https://localhost:60357 · API: https://localhost:7082')
  } else if (!stopping) {
    console.error('API se nepodařilo spustit do 90 sekund.')
    stop(1)
  }
}
