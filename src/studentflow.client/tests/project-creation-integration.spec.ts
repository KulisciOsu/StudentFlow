import { expect, test } from '@playwright/test'
import type { Page } from '@playwright/test'

// Izolovaná integrace routování a formuláře KAN-86 s autentizací KAN-46.
// Odpovědi identity jsou řízené; skutečné session/API ověřuje session.spec.ts.
async function mockSession(page: Page, accountType: 'STUDENT' | 'TEACHER') {
  let authenticated = true
  await page.route('**/api/auth/me', route => route.fulfill(authenticated ? {
    json: {
      user: { id: '1', displayName: 'Přihlášený uživatel', email: 'integration@example.test', accountType },
      session: { expiresAt: new Date(Date.now() + 3_600_000).toISOString(), persistent: false },
    },
  } : { status: 401 }))
  await page.route('**/api/auth/csrf', route => route.fulfill({
    json: { token: 'integration-csrf', headerName: 'X-CSRF-TOKEN' },
  }))
  await page.route('**/api/auth/logout', async route => {
    expect(route.request().method()).toBe('POST')
    expect(route.request().headers()['x-csrf-token']).toBe('integration-csrf')
    authenticated = false
    await route.fulfill({ status: 204 })
  })
}

test('anonymní návštěva založení projektu vyžaduje přihlášení', async ({ page }) => {
  await page.route('**/api/auth/me', route => route.fulfill({ status: 401 }))
  await page.goto('/projects/new')
  await expect(page).toHaveURL('/login')
  await expect(page.getByRole('heading', { name: 'Přihlášení', exact: true })).toBeVisible()
  await expect(page.getByLabel('Název projektu')).toHaveCount(0)
})

test('formulář čeká na ověření identity i při přímém otevření adresy', async ({ page }) => {
  let release!: () => void
  const gate = new Promise<void>(resolve => { release = resolve })
  await page.route('**/api/auth/me', async route => {
    await gate
    await route.fulfill({ status: 401 })
  })
  await page.goto('/projects/new', { waitUntil: 'domcontentloaded' })
  try {
    await expect(page.getByRole('status')).toHaveText('Ověřujeme přihlášení…')
    await expect(page.getByLabel('Název projektu')).toHaveCount(0)
  } finally {
    release()
  }
  await expect(page).toHaveURL('/login')
})

for (const [width, height] of [[390, 844], [820, 1180], [1280, 900]]) {
  test(`člen otevře formulář, vrátí se a odhlásí při ${width}px`, async ({ page }, testInfo) => {
    await page.setViewportSize({ width, height })
    await mockSession(page, 'STUDENT')
    await page.goto('/projects')
    await page.getByRole('button', { name: 'Založit projekt', exact: true }).click()
    await expect(page).toHaveURL('/projects/new')
    await expect(page.getByRole('heading', { name: 'Založit projekt', exact: true })).toBeVisible()
    await expect(page.getByText('Přihlášený uživatel', { exact: true })).toBeVisible()
    await expect(page.getByText('Petra Svobodová', { exact: true })).toHaveCount(0)
    await expect(page.getByRole('main')).toHaveCount(1)
    await expect(page.getByRole('button', { name: 'Odhlásit se', exact: true })).toHaveCount(1)
    expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true)

    await page.getByLabel('Název projektu').fill('Integrační projekt')
    await page.getByLabel('Plánovaný termín odevzdání').fill('2099-01-15')
    await page.getByRole('button', { name: 'Založit projekt', exact: true }).click()
    await expect(page.getByRole('alert')).toHaveText('Zadejte název týmu.')
    await page.getByLabel('Název týmu').fill('Integrační tým')
    await page.getByRole('button', { name: 'Založit projekt', exact: true }).click()
    await expect(page.getByRole('status')).toContainText('Integrační projekt')
    await page.screenshot({ path: testInfo.outputPath('project-form.png'), fullPage: true })

    await page.getByRole('link', { name: 'Moje projekty', exact: true }).click()
    await expect(page).toHaveURL('/projects')
    await page.goBack()
    await expect(page).toHaveURL('/projects/new')
    await expect(page.getByLabel('Název projektu')).toBeVisible()
    await page.reload()
    await expect(page.getByText('Přihlášený uživatel', { exact: true })).toBeVisible()
    await expect(page.getByLabel('Název projektu')).toBeVisible()
    await page.getByRole('button', { name: 'Odhlásit se', exact: true }).click()
    await expect(page).toHaveURL('/login')
    await page.goto('/projects/new')
    await expect(page).toHaveURL('/login')
    await expect(page.getByLabel('Název projektu')).toHaveCount(0)
  })
}

test('vyučující nemá nabídku založení projektu a přímý přístup je odmítnut', async ({ page }) => {
  await mockSession(page, 'TEACHER')
  await page.goto('/teaching')
  await expect(page.getByRole('heading', { name: 'Přehled vyučujícího' })).toBeVisible()
  await expect(page.getByRole('button', { name: 'Založit projekt' })).toHaveCount(0)
  await page.goto('/projects/new')
  await expect(page.getByRole('heading', { name: 'Přístup odepřen' })).toBeVisible()
  await expect(page.getByLabel('Název projektu')).toHaveCount(0)
  await page.getByRole('link', { name: 'Přejít na můj přehled' }).click()
  await expect(page).toHaveURL('/teaching')
})

test('výpadek ověření identity neotevře formulář projektu', async ({ page }) => {
  await page.route('**/api/auth/me', route => route.fulfill({ status: 503 }))
  await page.goto('/projects/new')
  await expect(page.getByRole('heading', { name: 'Přihlášení se nepodařilo ověřit' })).toBeVisible()
  await expect(page.getByLabel('Název projektu')).toHaveCount(0)
})
