import { expect, test } from '@playwright/test'
import type { APIRequestContext, Page } from '@playwright/test'

/// <summary>
/// Účty, které při E2E založí vývojový seed (scripts/e2e-api.mjs). Vedoucí vede ukázkový projekt 1 „Tým Sova“, člen je v něm běžným členem.
/// </summary>
const seedPassword = 'studentflow-e2e-heslo'
const leaderEmail = 'e2e-vedouci@studentflow.test'
const memberEmail = 'e2e-clen@studentflow.test'

/// <summary>
/// Heslo účtů registrovaných přímo v testu.
/// </summary>
const password = 'moje-dlouhe-heslo-2026'

const settingsPath = '/projects/1/settings'
const membersApi = '/api/projects/1/members'

// Testy sdílejí ukázkový projekt s testy nástěnky. Přidávají a odebírají jen čerstvě registrované účty,
// takže seedové členy ani úkoly nemění a mohou běžet ve všech prohlížečích současně.
// Proto nekontrolují celkový počet členů, který mezitím mohou měnit souběžné testy.

/// <summary>
/// Zaregistruje nový účet přes API a vrátí jeho e-mail a jedinečné jméno.
/// </summary>
async function createAccount(request: APIRequestContext, type: 'STUDENT' | 'TEACHER') {
  const id = crypto.randomUUID()
  const email = `members-${id}@example.test`
  const displayName = `Nový člen ${id.slice(0, 8)}`
  const csrf = await (await request.get('/api/auth/csrf')).json()
  expect((await request.post('/api/auth/register', {
    headers: { 'X-CSRF-TOKEN': csrf.token }, data: { displayName, email, password, accountType: type },
  })).status()).toBe(201)
  return { email, displayName }
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
/// Vrátí řádek seznamu členů podle e-mailu.
/// </summary>
function memberRow(page: Page, email: string) {
  return page.getByRole('listitem').filter({ hasText: email })
}

/// <summary>
/// Vyplní e-mail ve formuláři „Přidat člena“ a odešle ho.
/// </summary>
async function addMember(page: Page, email: string) {
  await page.getByRole('textbox', { name: 'E-mail registrovaného studenta' }).fill(email)
  await page.getByRole('button', { name: 'Přidat do týmu', exact: true }).click()
}

/// <summary>
/// Ověří, že stránka nejde posouvat do strany.
/// </summary>
async function noHorizontalOverflow(page: Page) {
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true)
}

for (const [width, height] of [[390, 844], [820, 1180], [1280, 900]]) {
  test(`vedoucí přidá studenta a po potvrzení ho odebere při ${width}px`, async ({ page, request }) => {
    await page.setViewportSize({ width, height })
    const student = await createAccount(request, 'STUDENT')
    await signIn(page, leaderEmail, seedPassword)

    await page.goto(settingsPath)
    await expect(page.getByRole('heading', { name: 'Nastavení projektu', exact: true })).toBeVisible()
    await expect(page.getByText('Tým Sova', { exact: true })).toBeVisible()
    const leaderRow = memberRow(page, leaderEmail)
    await expect(leaderRow.getByText('Testovací vedoucí (vy)')).toBeVisible()
    await expect(leaderRow.getByText('Vedoucí', { exact: true })).toBeVisible()
    await expect(leaderRow.getByRole('button')).toHaveCount(0)

    await addMember(page, student.email)
    await expect(page.getByText(`${student.displayName} je nyní členem týmu.`)).toBeVisible()
    await expect(page.getByRole('textbox', { name: 'E-mail registrovaného studenta' })).toHaveValue('')
    const row = memberRow(page, student.email)
    await expect(row.getByText(student.displayName, { exact: true })).toBeVisible()
    await expect(row.getByText('Člen', { exact: true })).toBeVisible()

    // Po obnovení stránky je člen pořád v seznamu, takže ho opravdu uložil server.
    await page.reload()
    await expect(row).toBeVisible()

    await row.getByRole('button', { name: 'Odebrat', exact: true }).click()
    const confirmation = page.getByRole('group', { name: `Opravdu odebrat? ${student.displayName}` })
    await expect(confirmation).toBeVisible()
    await expect(confirmation.getByRole('button', { name: 'Zrušit', exact: true })).toBeFocused()
    await noHorizontalOverflow(page)
    await confirmation.getByRole('button', { name: 'Zrušit', exact: true }).click()
    await expect(confirmation).toHaveCount(0)
    await expect(row).toBeVisible()

    await row.getByRole('button', { name: 'Odebrat', exact: true }).click()
    await confirmation.getByRole('button', { name: 'Odebrat', exact: true }).click()
    await expect(page.getByText(`${student.displayName} už není členem týmu.`)).toBeVisible()
    // Vidět je jen zpráva o poslední akci, ne už i starší potvrzení přidání.
    await expect(page.getByText(`${student.displayName} je nyní členem týmu.`)).toHaveCount(0)
    await expect(row).toHaveCount(0)
    await noHorizontalOverflow(page)

    await page.reload()
    await expect(leaderRow).toBeVisible()
    await expect(row).toHaveCount(0)
  })
}

test('vedoucí dostane srozumitelnou chybu u vyučujícího a neexistujícího účtu', async ({ page, request }) => {
  const teacher = await createAccount(request, 'TEACHER')
  const missingEmail = `missing-${crypto.randomUUID()}@example.test`
  await signIn(page, leaderEmail, seedPassword)
  await page.goto(settingsPath)
  const input = page.getByRole('textbox', { name: 'E-mail registrovaného studenta' })

  await addMember(page, teacher.email)
  await expect(page.getByRole('alert')).toHaveText('Do týmu lze přidat jen účet studenta.')
  await expect(input).toHaveValue(teacher.email)

  await addMember(page, missingEmail)
  await expect(page.getByRole('alert')).toHaveText('Účet s tímto e-mailem v StudentFlow neexistuje.')

  await expect(memberRow(page, teacher.email)).toHaveCount(0)
})

test('běžný člen nastavení nezmění ani obejitím stránky', async ({ page, request }) => {
  const student = await createAccount(request, 'STUDENT')
  await signIn(page, memberEmail, seedPassword)

  await page.goto(settingsPath)

  await expect(page.getByText('Nemáte oprávnění', { exact: true })).toBeVisible()
  await expect(page.getByRole('textbox', { name: 'E-mail registrovaného studenta' })).toHaveCount(0)
  await expect(page.getByRole('button', { name: 'Odebrat', exact: true })).toHaveCount(0)

  // Skrytí formuláře je jen pohodlí; rozhoduje server.
  const csrf = await (await page.request.get('/api/auth/csrf')).json()
  const response = await page.request.post(membersApi, { headers: { 'X-CSRF-TOKEN': csrf.token }, data: { email: student.email } })
  expect(response.status()).toBe(403)
  expect((await response.json()).code).toBe('PROJECT_LEADER_REQUIRED')
})

test('uživatel mimo projekt se o projektu nic nedozví', async ({ page, request }) => {
  const outsider = await createAccount(request, 'STUDENT')
  await signIn(page, outsider.email, password)

  await page.goto(settingsPath)

  await expect(page.getByRole('alert')).toHaveText('Projekt neexistuje nebo k němu nemáte přístup.')
  await expect(page.getByText('Tým Sova', { exact: true })).toHaveCount(0)
  expect((await page.request.get(membersApi)).status()).toBe(404)
})
