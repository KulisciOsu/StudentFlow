import { expect, test } from '@playwright/test'
import type { APIRequestContext, Locator, Page } from '@playwright/test'

/// <summary>
/// Účty, které při E2E založí vývojový seed (scripts/e2e-api.mjs) a zapojí do ukázkového projektu.
/// Člen převzal úkoly Jana Nováka, vedoucí úkoly Petry Svobodové.
/// </summary>
const seedPassword = 'studentflow-e2e-heslo'
const memberEmail = 'e2e-clen@studentflow.test'
const leaderEmail = 'e2e-vedouci@studentflow.test'

/// <summary>
/// Heslo účtů registrovaných přímo v testu.
/// </summary>
const password = 'moje-dlouhe-heslo-2026'

/// <summary>
/// Vlastní úkol člena a cizí úkol (řešitel Tomáš Dvořák) z ukázkových dat V005.
/// </summary>
const ownTask = 'Návrh databázového modelu'
const foreignTask = 'Formulář rezervace'

interface BoardTask {
  id: number
  title: string
  columnId: number
  position: number
}

interface BoardColumn {
  id: number
  title: string
}

/// <summary>
/// Zaregistruje nový účet přes API a vrátí jeho e-mail.
/// </summary>
async function createAccount(request: APIRequestContext, type: 'STUDENT' | 'TEACHER') {
  const email = `board-${crypto.randomUUID()}@example.test`
  const csrf = await (await request.get('/api/auth/csrf')).json()
  expect((await request.post('/api/auth/register', {
    headers: { 'X-CSRF-TOKEN': csrf.token }, data: { displayName: 'Testovací uživatel', email, password, accountType: type },
  })).status()).toBe(201)
  return email
}

/// <summary>
/// Přihlásí uživatele přes přihlašovací formulář.
/// </summary>
async function signIn(page: Page, email: string, accountPassword: string) {
  await page.goto('/login')
  await page.getByLabel('E-mail', { exact: true }).fill(email)
  await page.getByLabel('Heslo', { exact: true }).fill(accountPassword)
  await page.getByRole('button', { name: 'Přihlásit se', exact: true }).click()
  await expect(page.getByRole('button', { name: 'Odhlásit se', exact: true })).toBeVisible()
}

/// <summary>
/// Otevře nástěnku a počká na první kartu. Vrátí dobu načtení v milisekundách.
/// </summary>
async function openBoard(page: Page) {
  const start = Date.now()
  await page.goto('/board')
  await expect(page.locator('.task-card').first()).toBeVisible()
  return Date.now() - start
}

/// <summary>
/// Vrátí sloupec nástěnky podle názvu.
/// </summary>
function column(page: Page, title: string) {
  return page.locator('.kanban-column').filter({ has: page.locator('h3', { hasText: title }) })
}

/// <summary>
/// Vrátí kartu úkolu podle názvu.
/// </summary>
function card(scope: Page | Locator, title: string) {
  return scope.locator('.task-card', { hasText: title })
}

/// <summary>
/// Přetáhne kartu myší na hlavičku sloupce, takže se karta zařadí na jeho konec.
/// </summary>
async function dragCard(page: Page, title: string, columnTitle: string) {
  const source = (await card(page, title).boundingBox())!
  const target = (await column(page, columnTitle).boundingBox())!
  await page.mouse.move(source.x + 20, source.y + 20)
  await page.mouse.down()
  await page.mouse.move(source.x + 40, source.y + 30, { steps: 3 })
  await page.mouse.move(target.x + target.width / 2, target.y + 20, { steps: 12 })
}

/// <summary>
/// Načte úkoly a sloupce ukázkového projektu přes API pod přihlášeným uživatelem.
/// </summary>
async function loadBoard(page: Page) {
  const tasks: BoardTask[] = await (await page.request.get('/api/projects/1/tasks')).json()
  const columns: BoardColumn[] = await (await page.request.get('/api/projects/1/columns')).json()
  return { tasks, columns }
}

/// <summary>
/// Uloží sloupec a pozici úkolu přes API a vrátí stavový kód.
/// </summary>
async function saveMove(page: Page, taskId: number, columnId: number, position: number) {
  const response = await page.request.put(`/api/projects/1/tasks/${taskId}/position`, { data: { columnId, position } })
  return response.status()
}

// Testy sdílejí ukázkový projekt. Úkoly mění jen Chromium a každý přesun na konci vrátí zpět.
test.describe.configure({ mode: 'serial' })

test('člen načte nástěnku do 2 s a filtruje úkoly', async ({ page }) => {
  await signIn(page, memberEmail, seedPassword)

  const loadTime = await openBoard(page)

  expect(loadTime).toBeLessThan(2000)
  for (const title of ['K řešení', 'Rozpracováno', 'Ke kontrole', 'Hotovo']) await expect(column(page, title)).toBeVisible()
  await expect(page.locator('.kanban-column .task-card')).toHaveCount(10)

  await page.getByLabel('Hledat úkol').fill('rezervace')
  await expect(page.locator('.kanban-column .task-card')).toHaveCount(1)
  await expect(card(page, foreignTask)).toBeVisible()

  await page.getByLabel('Hledat úkol').fill('')
  await page.getByLabel('Řešitel').selectOption('Lucie Černá')
  await expect(page.locator('.kanban-column .task-card')).toHaveCount(3)

  await page.getByLabel('Řešitel').selectOption('')
  await page.getByLabel('Jen moje úkoly').check()
  await expect(page.locator('.kanban-column .task-card')).toHaveCount(2)
  await expect(card(page, ownTask)).toBeVisible()

  await page.getByLabel('Hledat úkol').fill('neexistuje')
  await expect(page.getByText('Žádný úkol neodpovídá filtrům.')).toBeVisible()
})

test('přesunutý úkol se projeví do 0,5 s, uloží do 2 s a zůstane po obnovení', async ({ page, browserName }) => {
  test.skip(browserName !== 'chromium', 'Sdílená ukázková data mění jen Chromium.')
  await signIn(page, memberEmail, seedPassword)
  await openBoard(page)
  const { tasks } = await loadBoard(page)
  const original = tasks.find(task => task.title === ownTask)!

  try {
    await dragCard(page, ownTask, 'Ke kontrole')
    const saved = page.waitForResponse(response => response.url().endsWith('/position') && response.request().method() === 'PUT')
    const dropped = Date.now()
    await page.mouse.up()

    await expect(card(column(page, 'Ke kontrole'), ownTask)).toBeVisible()
    expect(Date.now() - dropped).toBeLessThan(500)
    expect((await saved).status()).toBe(204)
    expect(Date.now() - dropped).toBeLessThan(2000)

    await page.reload()
    await expect(card(column(page, 'Ke kontrole'), ownTask)).toBeVisible()
  } finally {
    expect(await saveMove(page, original.id, original.columnId, original.position)).toBe(204)
  }
})

test('při selhání uložení se karta vrátí a zobrazí se chyba', async ({ page, browserName }) => {
  test.skip(browserName !== 'chromium', 'Sdílená ukázková data mění jen Chromium.')
  await signIn(page, memberEmail, seedPassword)
  await openBoard(page)
  await page.route('**/api/projects/1/tasks/*/position', route => route.fulfill({ status: 500 }))

  await dragCard(page, ownTask, 'Ke kontrole')
  await page.mouse.up()

  await expect(page.getByText(/se nepodařilo uložit/)).toBeVisible()
  await expect(card(column(page, 'Rozpracováno'), ownTask)).toBeVisible()
  await expect(card(column(page, 'Ke kontrole'), ownTask)).toHaveCount(0)

  await page.unroute('**/api/projects/1/tasks/*/position')
  await page.reload()
  await expect(card(column(page, 'Rozpracováno'), ownTask)).toBeVisible()
})

test('člen přesouvá jen své úkoly a cizí přesun API odmítne', async ({ page }) => {
  await signIn(page, memberEmail, seedPassword)
  await openBoard(page)
  const { tasks, columns } = await loadBoard(page)
  const foreign = tasks.find(task => task.title === foreignTask)!
  const otherColumn = columns.find(item => item.id !== foreign.columnId)!

  await expect(card(page, ownTask)).not.toHaveClass(/task-card--locked/)
  await expect(card(page, foreignTask)).toHaveClass(/task-card--locked/)
  expect(await saveMove(page, foreign.id, otherColumn.id, 0)).toBe(403)
})

test('vedoucí smí přesunout i cizí úkol', async ({ page, browserName }) => {
  test.skip(browserName !== 'chromium', 'Sdílená ukázková data mění jen Chromium.')
  await signIn(page, leaderEmail, seedPassword)
  await openBoard(page)
  const { tasks, columns } = await loadBoard(page)
  const foreign = tasks.find(task => task.title === foreignTask)!
  const otherColumn = columns.find(item => item.id !== foreign.columnId)!

  await expect(page.locator('.task-card--locked')).toHaveCount(0)

  try {
    expect(await saveMove(page, foreign.id, otherColumn.id, 0)).toBe(204)
  } finally {
    expect(await saveMove(page, foreign.id, foreign.columnId, foreign.position)).toBe(204)
  }
})

test('vyučující má nástěnku jen pro čtení a přesun API odmítne', async ({ page, request }) => {
  await signIn(page, await createAccount(request, 'TEACHER'), password)
  await openBoard(page)
  const { tasks, columns } = await loadBoard(page)
  const task = tasks.find(item => item.title === foreignTask)!
  const otherColumn = columns.find(item => item.id !== task.columnId)!

  await expect(page.getByText('Jen pro čtení', { exact: true })).toBeVisible()
  await expect(page.getByLabel('Jen moje úkoly')).toHaveCount(0)
  await expect(page.locator('.kanban-column .task-card:not(.task-card--locked)')).toHaveCount(0)
  expect(await saveMove(page, task.id, otherColumn.id, 0)).toBe(403)
})

test('uživatel mimo projekt nemá k nástěnce přístup', async ({ page, request }) => {
  await signIn(page, await createAccount(request, 'STUDENT'), password)

  await page.goto('/board')

  await expect(page.getByText('Do tohoto projektu nemáte přístup.')).toBeVisible()
  expect((await page.request.get('/api/projects/1/tasks')).status()).toBe(403)
})

test('prázdná nástěnka zobrazí prázdný stav', async ({ page }) => {
  await signIn(page, memberEmail, seedPassword)
  await page.route('**/api/projects/1/tasks', route => route.fulfill({ json: [] }))

  await page.goto('/board')

  await expect(page.getByText('Na nástěnce zatím nejsou žádné úkoly.')).toBeVisible()
  await expect(column(page, 'K řešení').getByText('Žádné úkoly')).toBeVisible()
})

test('na mobilu jsou sloupce v záložkách a přesun jen u vlastních úkolů', async ({ page }) => {
  await page.setViewportSize({ width: 390, height: 844 })
  await signIn(page, memberEmail, seedPassword)
  await page.goto('/board')

  await page.getByRole('tab', { name: /Rozpracováno/ }).click()
  await expect(card(page, ownTask)).toBeVisible()
  await expect(page.getByLabel(`Přesunout úkol ${ownTask}`)).toBeVisible()
  await page.getByRole('tab', { name: /K řešení/ }).click()
  await expect(card(page, foreignTask)).toBeVisible()
  await expect(page.getByLabel(`Přesunout úkol ${foreignTask}`)).toHaveCount(0)
  await expect(page.getByRole('button', { name: 'Nový úkol' })).toBeVisible()
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true)
})
