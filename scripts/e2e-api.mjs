import { spawn } from 'node:child_process'
import { randomUUID } from 'node:crypto'
import { readFile, readdir } from 'node:fs/promises'
import { createRequire } from 'node:module'
import { dirname, join, resolve } from 'node:path'
import { fileURLToPath } from 'node:url'

const root = resolve(dirname(fileURLToPath(import.meta.url)), '..')
const require = createRequire(join(root, 'src/studentflow.client/package.json'))
const { Client } = require('pg')
const connectionString = process.env.STUDENTFLOW_E2E_DATABASE_URL
if (!connectionString) throw new Error('Nastavte STUDENTFLOW_E2E_DATABASE_URL pro vyhrazenou testovací PostgreSQL 17.')
const databaseName = `studentflow_e2e_${randomUUID().replaceAll('-', '')}`
const admin = new Client({ connectionString, connectionTimeoutMillis: 5000 })
let databaseCreated = false
let api
let stopping = false
const stop = () => {
  stopping = true
  api?.kill('SIGTERM')
}
process.on('SIGINT', stop)
process.on('SIGTERM', stop)

try {
  await admin.connect()
  const version = await admin.query('SHOW server_version_num')
  if (Math.trunc(Number(version.rows[0].server_version_num) / 10000) !== 17) throw new Error('E2E vyžaduje PostgreSQL 17.')
  await admin.query(`CREATE DATABASE ${databaseName}`)
  databaseCreated = true
  const testDatabaseUrl = new URL(connectionString)
  testDatabaseUrl.pathname = `/${databaseName}`
  testDatabaseUrl.searchParams.delete('database')
  testDatabaseUrl.searchParams.delete('dbname')
  const client = new Client({ connectionString: testDatabaseUrl.toString(), connectionTimeoutMillis: 5000 })
  try {
    if (client.connectionParameters.database !== databaseName) throw new Error('E2E připojení musí směřovat do izolované databáze.')
    await client.connect()
    const directory = join(root, 'database/migrations')
    for (const file of (await readdir(directory)).filter(name => /^V\d+__.*\.sql$/.test(name)).sort())
      await client.query(await readFile(join(directory, file), 'utf8'))
    const params = client.connectionParameters
    const quote = value => `"${String(value).replaceAll('"', '""')}"`
    const backendConnection = `Host=${quote(params.host)};Port=${params.port};Database=${databaseName};Username=${quote(params.user)};Password=${quote(params.password)};SSL Mode=Disable`
    if (!stopping) {
      api = spawn(process.execPath, [join(root, 'scripts/dev.mjs'), 'api'], {
        cwd: root, stdio: 'inherit', env: { ...process.env, ConnectionStrings__DefaultConnection: backendConnection,
          Auth__RegistrationRateLimit__PermitLimit: '200', Auth__LoginRateLimit__PermitLimit: '200',
          // Účty pro E2E testy nástěnky; seed je zapojí do ukázkového projektu (tests/board.spec.ts).
          DevelopmentSeed__Enabled: 'true', DevelopmentSeed__Password: 'studentflow-e2e-heslo',
          DevelopmentSeed__MemberEmail: 'e2e-clen@studentflow.test', DevelopmentSeed__LeaderEmail: 'e2e-vedouci@studentflow.test',
          DevelopmentSeed__TeacherEmail: 'e2e-ucitel@studentflow.test' },
      })
      const code = await new Promise((resolveExit, reject) => {
        api.on('error', reject)
        api.on('exit', status => resolveExit(status ?? (stopping ? 0 : 1)))
      })
      if (!stopping) process.exitCode = code || 1
    }
  } finally {
    await client.end()
  }
} finally {
  if (databaseCreated) await admin.query(`DROP DATABASE ${databaseName} WITH (FORCE)`)
  await admin.end()
}
