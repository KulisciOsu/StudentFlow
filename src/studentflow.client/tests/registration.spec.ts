import { expect, test } from '@playwright/test'
import type { Page } from '@playwright/test'

async function fillRegistration(page: Page, email: string, type: 'Člen' | 'Vyučující' = 'Člen') {
  await page.goto('/register')
  await page.getByRole('textbox', { name: 'Jméno', exact: true }).fill('Jan')
  await page.getByRole('textbox', { name: 'Příjmení', exact: true }).fill('Novák')
  await page.getByRole('textbox', { name: 'E-mail', exact: true }).fill(email)
  await page.getByLabel(/^Heslo\s*\*?$/u).fill('TestHeslo1!')
  await page.getByLabel(/^Heslo znovu\s*\*?$/u).fill('TestHeslo1!')
  await page.getByRole('radio', { name: new RegExp(`^${type}`) }).check()
  await page.getByRole('checkbox', { name: /Seznámil/ }).check()
}

for (const type of ['Člen', 'Vyučující'] as const) {
  test(`registrace typu ${type} funguje proti API a PostgreSQL`, async ({ page }) => {
    await fillRegistration(page, `registration-${crypto.randomUUID()}@example.test`, type)
    const result = page.waitForResponse(response => response.url().endsWith('/api/auth/register') && response.request().method() === 'POST')
    await page.getByRole('button', { name: 'Vytvořit účet', exact: true }).click()
    expect((await result).status()).toBe(201)
    await expect(page).toHaveURL('/login')
    await expect(page.getByRole('status')).toContainText('Účet je vytvořený. Teď se můžete přihlásit.')
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
  await expect(page.getByRole('alert')).toContainText('S tímto e-mailem teď účet nelze vytvořit.')
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
  await expect(page.getByRole('alert')).toContainText('Ještě prosím zkontrolujte označené údaje.')
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

test('potvrzení zásad není předvybrané, bez něj se registrace neodešle a odkaz zachová formulář', async ({ page }) => {
  let posts = 0
  page.on('request', request => { if (request.url().endsWith('/api/auth/register')) posts++ })
  const email = `privacy-${crypto.randomUUID()}@example.test`
  await page.goto('/register')
  await expect(page.getByRole('checkbox', { name: /Seznámil/ })).not.toBeChecked()
  await fillRegistration(page, email)
  const privacy = page.getByRole('checkbox', { name: /Seznámil/ })
  await privacy.uncheck()
  await page.getByRole('button', { name: 'Vytvořit účet', exact: true }).click()
  await expect(page.getByRole('alert')).toContainText('zásadami ochrany osobních údajů')
  expect(posts).toBe(0)
  const popup = page.waitForEvent('popup')
  await page.getByRole('link', { name: 'zásadami ochrany osobních údajů', exact: true }).click()
  const policy = await popup
  await expect(policy.getByRole('heading', { name: 'Zásady ochrany osobních údajů', exact: true })).toBeVisible()
  await expect(policy.getByText('StudentFlow · verze 2026-11-20.2')).toBeVisible()
  await expect(policy.getByText('Demonstrační zásady školního projektu.', { exact: true })).toBeVisible()
  await policy.close()
  await expect(page.getByRole('textbox', { name: 'E-mail', exact: true })).toHaveValue(email)
  await expect(privacy).not.toBeChecked()
})

test('jméno a příjmení jsou samostatné, bílé znaky brání odeslání a souhrn zaměří správné pole', async ({ page }) => {
  let posts = 0
  page.on('request', request => { if (request.url().endsWith('/api/auth/register')) posts++ })
  await fillRegistration(page, `name-${crypto.randomUUID()}@example.test`)
  const firstName = page.getByRole('textbox', { name: 'Jméno', exact: true })
  const lastName = page.getByRole('textbox', { name: 'Příjmení', exact: true })
  await expect(firstName).toHaveAttribute('autocomplete', 'given-name')
  await expect(lastName).toHaveAttribute('autocomplete', 'family-name')
  await firstName.fill('Jan Pavel')
  await lastName.fill('Novák\u00a0Svoboda')
  await page.getByRole('button', { name: 'Vytvořit účet', exact: true }).click()
  await expect(firstName).toHaveAttribute('aria-invalid', 'true')
  await expect(lastName).toHaveAttribute('aria-invalid', 'true')
  await page.getByRole('alert').getByRole('link', { name: /^Příjmení/ }).click()
  await expect(lastName).toBeFocused()
  expect(posts).toBe(0)
})

test('limit jména se vysvětlí až při chybě, 50 Unicode znaků a jednoznakové příjmení projdou', async ({ page }) => {
  let posts = 0
  page.on('request', request => { if (request.url().endsWith('/api/auth/register')) posts++ })
  await fillRegistration(page, `name-limit-${crypto.randomUUID()}@example.test`)
  await expect(page.getByText(/(?:nejvýše|až|–|-)\s*(?:50|160)\s*znaků/u)).toHaveCount(0)
  const firstName = page.getByRole('textbox', { name: 'Jméno', exact: true })
  await firstName.fill('𐐀'.repeat(51))
  await page.getByRole('textbox', { name: 'Příjmení', exact: true }).fill('李')
  await page.getByRole('button', { name: 'Vytvořit účet', exact: true }).click()
  await expect(page.getByRole('alert')).toContainText('Jméno může mít nejvýše 50 znaků.')
  expect(posts).toBe(0)
  await firstName.fill('𐐀'.repeat(50))
  const response = page.waitForResponse(value => value.url().endsWith('/api/auth/register') && value.request().method() === 'POST')
  await page.getByRole('button', { name: 'Vytvořit účet', exact: true }).click()
  expect((await response).status()).toBe(201)
  await expect(page).toHaveURL('/login')
})

test('číslice a speciální znaky v obou částech jména brání registraci', async ({ page }) => {
  let posts = 0
  page.on('request', request => { if (request.url().endsWith('/api/auth/register')) posts++ })
  await fillRegistration(page, `letters-${crypto.randomUUID()}@example.test`)
  const firstName = page.getByRole('textbox', { name: 'Jméno', exact: true })
  const lastName = page.getByRole('textbox', { name: 'Příjmení', exact: true })
  await firstName.fill('Jan1')
  await lastName.fill('Novák-Svoboda')
  await page.getByRole('button', { name: 'Vytvořit účet', exact: true }).click()
  await expect(page.getByRole('alert')).toContainText('Jméno může obsahovat pouze písmena a diakritiku.')
  await expect(page.getByRole('alert')).toContainText('Příjmení může obsahovat pouze písmena a diakritiku.')
  expect(posts).toBe(0)
  await firstName.fill('Žofie')
  await lastName.fill('Nova\u0301k')
  const response = page.waitForResponse(value => value.url().endsWith('/api/auth/register') && value.request().method() === 'POST')
  await page.getByRole('button', { name: 'Vytvořit účet', exact: true }).click()
  expect((await response).status()).toBe(201)
  await expect(page).toHaveURL('/login')
})
