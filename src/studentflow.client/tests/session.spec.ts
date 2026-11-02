import { expect, test } from '@playwright/test'
import type { APIRequestContext, Page } from '@playwright/test'

const password = 'moje-dlouhe-heslo-2026'
const cookieName = '__Host-StudentFlow.Session'
async function createAccount(request: APIRequestContext, type: 'STUDENT' | 'TEACHER' = 'STUDENT') {
  const email = `session-${crypto.randomUUID()}@example.test`
  const csrf = await (await request.get('/api/auth/csrf')).json()
  expect((await request.post('/api/auth/register', {
    headers: { 'X-CSRF-TOKEN': csrf.token }, data: { displayName: 'Testovací uživatel', email, password, accountType: type },
  })).status()).toBe(201)
  return email
}
async function fillLogin(page: Page, email: string) {
  await page.goto('/login')
  await page.getByLabel('E-mail', { exact: true }).fill(email)
  await page.getByLabel('Heslo', { exact: true }).fill(password)
}
async function signIn(page: Page, email: string) {
  await fillLogin(page, email)
  await page.getByRole('button', { name: 'Přihlásit se', exact: true }).click()
  await expect(page.getByRole('button', { name: 'Odhlásit se', exact: true })).toBeVisible()
}

for (const [type, home] of [['STUDENT', '/projects'], ['TEACHER', '/teaching']] as const) {
  test(`login ${type}, obnova identity a odmítnutí opačného pohledu`, async ({ page, request }) => {
    const email = await createAccount(request, type)
    await fillLogin(page, email)
    if (type === 'TEACHER') await page.getByRole('checkbox', { name: /^Zůstat přihlášen/ }).check()
    await page.getByLabel('Heslo', { exact: true }).press('Enter')
    await expect(page).toHaveURL(home)
    await expect(page.getByText('Testovací uživatel', { exact: true })).toBeVisible()
    const cookie = (await page.context().cookies()).find(item => item.name === cookieName)!
    expect(cookie.httpOnly).toBe(true); expect(cookie.secure).toBe(true); expect(cookie.sameSite).toBe('Lax')
    expect(type === 'TEACHER' ? cookie.expires > Date.now() / 1000 + 29 * 86400 : cookie.expires === -1).toBe(true)
    await page.reload(); await expect(page).toHaveURL(home)
    await expect(page.getByText('Testovací uživatel', { exact: true })).toBeVisible()
    await page.goto(type === 'STUDENT' ? '/teaching' : '/projects')
    await expect(page.getByRole('heading', { name: 'Přístup odepřen' })).toBeVisible()
    await expect(page.getByRole('button', { name: 'Odhlásit se', exact: true })).toBeVisible()
    await page.getByRole('link', { name: 'Přejít na můj přehled' }).click()
    await expect(page).toHaveURL(home)
  })
}

test('neplatné přihlášení zachová e-mail, smaže heslo a nic nepřihlásí', async ({ page }) => {
  const email = `missing-${crypto.randomUUID()}@example.test`
  await fillLogin(page, email); await page.getByRole('button', { name: 'Přihlásit se', exact: true }).click()
  await expect(page.getByRole('alert')).toContainText('E-mail nebo heslo nejsou správné.')
  await expect(page.getByLabel('E-mail', { exact: true })).toHaveValue(email)
  await expect(page.getByLabel('Heslo', { exact: true })).toHaveValue('')
  await expect(page.getByRole('alert')).toBeFocused()
  expect((await page.context().cookies()).some(cookie => cookie.name === cookieName)).toBe(false)
})

test('ověření identity skryje soukromý obsah do odpovědi serveru', async ({ page, request }) => {
  await signIn(page, await createAccount(request))
  let release!: () => void
  const gate = new Promise<void>(resolve => { release = resolve })
  await page.route('**/api/auth/me', async route => { await gate; await route.continue().catch(() => {}) })
  await page.reload({ waitUntil: 'domcontentloaded' })
  await expect(page.getByRole('status')).toHaveText('Ověřujeme přihlášení…')
  await expect(page.getByText('Testovací uživatel', { exact: true })).toHaveCount(0)
  release()
  await expect(page.getByText('Testovací uživatel', { exact: true })).toBeVisible()
})

test('výpadek ověření se nevydává za odhlášení a dovolí nové ověření', async ({ page }) => {
  await page.route('**/api/auth/me', route => route.fulfill({ status: 503, contentType: 'application/problem+json', body: '{}' }))
  await page.goto('/projects')
  await expect(page.getByRole('heading', { name: 'Přihlášení se nepodařilo ověřit' })).toBeVisible()
  await expect(page.getByRole('button', { name: 'Přihlásit se', exact: true })).toHaveCount(0)
  await expect(page.getByRole('heading', { name: 'Projekty', exact: true })).toHaveCount(0)
  await page.unroute('**/api/auth/me')
  await page.getByRole('button', { name: 'Zkusit znovu', exact: true }).click()
  await expect(page).toHaveURL('/login')
})

test('logout zneplatní cookie na serveru a tlačítko Zpět neodhalí přehled', async ({ page, request }) => {
  await signIn(page, await createAccount(request))
  const cookie = (await page.context().cookies()).find(item => item.name === cookieName)!
  await page.getByRole('link', { name: 'Můj přehled' }).click()
  await page.getByRole('button', { name: 'Odhlásit se', exact: true }).click()
  await expect(page).toHaveURL('/login'); await expect(page.getByRole('status')).toHaveText('Byli jste odhlášeni.')
  expect((await request.get('/api/auth/me', { headers: { Cookie: `${cookieName}=${cookie.value}` } })).status()).toBe(401)
  await page.goBack(); await expect(page).toHaveURL('/login')
  await expect(page.getByText('Testovací uživatel', { exact: true })).toHaveCount(0)
})

test('nepotvrzený logout skryje identitu a nabídne opakování', async ({ page, request }) => {
  await signIn(page, await createAccount(request))
  let attempts = 0
  await page.route('**/api/auth/logout', route => { attempts++; return route.fulfill({ status: 503, contentType: 'application/problem+json', body: '{}' }) })
  await page.getByRole('button', { name: 'Odhlásit se', exact: true }).click()
  await expect(page.getByRole('heading', { name: 'Odhlášení se nepodařilo potvrdit' })).toBeVisible()
  await expect(page.getByText('Testovací uživatel', { exact: true })).toHaveCount(0)
  expect(attempts).toBe(1)
  await page.unroute('**/api/auth/logout')
  await page.getByRole('button', { name: 'Zkusit odhlášení znovu' }).click()
  await expect(page).toHaveURL('/login')
})

test('nejistý login neopakuje POST a nabídne bezpečné ověření me', async ({ page }) => {
  let attempts = 0
  await page.route('**/api/auth/login', route => { attempts++; return route.fulfill({ status: 503, contentType: 'application/problem+json', body: '{}' }) })
  await fillLogin(page, 'uncertain@example.test'); await page.getByRole('button', { name: 'Přihlásit se', exact: true }).click()
  await expect(page.getByRole('alert')).toContainText('Přihlášení se nepodařilo potvrdit.')
  expect(attempts).toBe(1)
  await page.getByRole('button', { name: 'Ověřit přihlášení', exact: true }).click()
  await expect(page.getByRole('heading', { name: 'Přihlášení', exact: true })).toBeVisible()
  await expect(page.getByRole('button', { name: 'Odhlásit se', exact: true })).toHaveCount(0)
})

test('serverem zneplatněná session po refreshi neobnoví soukromý obsah', async ({ page, request }) => {
  await signIn(page, await createAccount(request))
  const api = page.context().request
  const csrf = await (await api.get('/api/auth/csrf')).json()
  expect((await api.post('/api/auth/logout', { headers: { 'X-CSRF-TOKEN': csrf.token } })).status()).toBe(204)
  await page.reload(); await expect(page).toHaveURL('/login')
  await expect(page.getByText('Testovací uživatel', { exact: true })).toHaveCount(0)
})

test('logout odstraní identitu také v jiné kartě stejného prohlížeče', async ({ page, context, request }) => {
  await signIn(page, await createAccount(request))
  const other = await context.newPage(); await other.goto('/projects')
  await expect(other.getByText('Testovací uživatel', { exact: true })).toBeVisible()
  await page.getByRole('button', { name: 'Odhlásit se', exact: true }).click()
  await expect(other).toHaveURL('/login')
  await expect(other.getByText('Testovací uživatel', { exact: true })).toHaveCount(0)
  await other.close()
})

for (const width of [320, 390]) {
  test(`přihlášení funguje klávesnicí i při šířce ${width}px`, async ({ page }) => {
    await page.setViewportSize({ width, height: 740 }); await page.goto('/login')
    expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true)
    await page.getByLabel('Heslo', { exact: true }).fill(password)
    await page.getByRole('button', { name: 'Zobrazit heslo', exact: true }).click()
    await expect(page.getByLabel('Heslo', { exact: true })).toHaveAttribute('type', 'text')
    await page.getByRole('checkbox', { name: /^Zůstat přihlášen/ }).focus(); await page.keyboard.press('Space')
    await expect(page.getByRole('checkbox', { name: /^Zůstat přihlášen/ })).toBeChecked()
  })
}
