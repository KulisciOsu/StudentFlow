import { expect, test } from '@playwright/test'
import type { Page } from '@playwright/test'

async function fillRegistration(page: Page, email: string, type: 'Člen' | 'Vyučující' = 'Člen') {
  await page.goto('/register')
  await page.getByRole('textbox', { name: 'Jméno a příjmení', exact: true }).fill('Jan Novák')
  await page.getByRole('textbox', { name: 'E-mail', exact: true }).fill(email)
  await page.getByLabel(/^Heslo\s*\*?$/u).fill('moje-dlouhe-heslo-2026')
  await page.getByLabel(/^Heslo znovu\s*\*?$/u).fill('moje-dlouhe-heslo-2026')
  await page.getByRole('radio', { name: new RegExp(`^${type}`) }).check()
}

for (const type of ['Člen', 'Vyučující'] as const) {
  test(`registrace typu ${type} funguje proti API a PostgreSQL`, async ({ page }) => {
    await fillRegistration(page, `registration-${crypto.randomUUID()}@example.test`, type)
    const result = page.waitForResponse(response => response.url().endsWith('/api/auth/register') && response.request().method() === 'POST')
    await page.getByRole('button', { name: 'Vytvořit účet', exact: true }).click()
    expect((await result).status()).toBe(201)
    await expect(page).toHaveURL('/login')
    await expect(page.getByRole('status')).toContainText('Účet byl vytvořen. Nyní se přihlaste.')
    await expect(page.getByLabel('Heslo', { exact: true })).toHaveValue('')
    expect((await page.context().cookies()).some(cookie => cookie.name === '__Host-StudentFlow.Session')).toBe(false)
  })
}

test('duplicita zůstane chybou, zachová údaje a vymaže hesla', async ({ page }) => {
  const email = `duplicate-${crypto.randomUUID()}@example.test`
  await fillRegistration(page, email)
  await page.getByRole('button', { name: 'Vytvořit účet', exact: true }).click()
  await expect(page).toHaveURL('/login')
  await expect(page.getByRole('textbox', { name: 'E-mail', exact: true })).toHaveValue(email)
  await fillRegistration(page, email.toUpperCase())
  await page.getByRole('button', { name: 'Vytvořit účet', exact: true }).click()
  await expect(page.getByRole('alert')).toContainText('Účet s tímto e-mailem už existuje.')
  await expect(page.getByRole('textbox', { name: 'E-mail', exact: true })).toHaveValue(email.toUpperCase())
  await expect(page.getByLabel(/^Heslo\s*\*?$/u)).toHaveValue('')
  await expect(page.getByLabel(/^Heslo znovu\s*\*?$/u)).toHaveValue('')
  await expect(page.getByRole('heading', { name: 'Účet byl vytvořen' })).toHaveCount(0)
})

test('neplatný formulář neposílá požadavek a typy jsou výlučné', async ({ page }) => {
  let posts = 0
  page.on('request', request => { if (request.url().endsWith('/api/auth/register')) posts++ })
  await page.goto('/register')
  await page.getByRole('button', { name: 'Vytvořit účet', exact: true }).click()
  await expect(page.getByRole('alert')).toContainText('Opravte označená pole.')
  await expect(page.getByRole('alert')).toBeFocused()
  await page.getByRole('radio', { name: /^Člen/ }).check()
  await page.getByRole('radio', { name: /^Vyučující/ }).check()
  await expect(page.getByRole('radio', { name: /^Člen/ })).not.toBeChecked()
  await expect(page.getByRole('radio', { name: /^Vyučující/ })).toBeChecked()
  expect(posts).toBe(0)
})

test('výpadek spojení neoznámí úspěch a zápis automaticky neopakuje', async ({ page }) => {
  let attempts = 0
  await page.route('**/api/auth/register', route => { attempts++; return route.abort('failed') })
  await fillRegistration(page, `offline-${crypto.randomUUID()}@example.test`)
  await page.getByRole('button', { name: 'Vytvořit účet', exact: true }).click()
  await expect(page.getByRole('alert')).toContainText('Vytvoření účtu se nepodařilo potvrdit.')
  await expect(page.getByRole('button', { name: 'Vytvořit účet', exact: true })).toBeEnabled()
  await expect(page.getByRole('heading', { name: 'Účet byl vytvořen' })).toHaveCount(0)
  expect(attempts).toBe(1)
})

for (const width of [320, 390]) {
  test(`formulář má dostupná tlačítka a nepřetéká při šířce ${width}px`, async ({ page }) => {
    await page.setViewportSize({ width, height: 740 })
    await page.goto('/register')
    await expect(page.getByRole('button', { name: 'Vytvořit účet', exact: true })).toBeVisible()
    expect(await page.evaluate(() => document.documentElement.scrollWidth <= window.innerWidth)).toBe(true)
    await page.getByLabel(/^Heslo\s*\*?$/u).fill('heslo-pro-kontrolu')
    await page.getByRole('button', { name: 'Zobrazit heslo', exact: true }).click()
    await expect(page.getByLabel(/^Heslo\s*\*?$/u)).toHaveAttribute('type', 'text')
    await page.getByRole('button', { name: 'Skrýt heslo', exact: true }).click()
    await expect(page.getByLabel(/^Heslo\s*\*?$/u)).toHaveAttribute('type', 'password')
  })
}
