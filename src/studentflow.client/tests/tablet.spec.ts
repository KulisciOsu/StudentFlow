import { expect, test } from '@playwright/test'
import type { Locator, Page, TestInfo } from '@playwright/test'

test.use({ hasTouch: true })

async function noHorizontalOverflow(page: Page) {
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true)
}

async function box(locator: Locator) {
  const value = await locator.boundingBox()
  expect(value).not.toBeNull()
  return value!
}

async function preview(page: Page, testInfo: TestInfo, name: string) {
  const path = testInfo.outputPath(`${name}.png`)
  await page.screenshot({ path, fullPage: true })
  await testInfo.attach(name, { path, contentType: 'image/png' })
}

const tablets = [
  { width: 744, height: 1133 },
  { width: 768, height: 1024 },
  { width: 820, height: 1180 },
  { width: 1024, height: 768 },
  { width: 1180, height: 820 },
]

for (const viewport of tablets) {
  test(`tablet ${viewport.width}×${viewport.height}: formuláře a dotykové ovládání`, async ({ page }, testInfo) => {
    await page.setViewportSize(viewport)
    await page.goto('/register')
    const name = page.getByRole('textbox', { name: 'Jméno a příjmení', exact: true })
    const email = page.getByRole('textbox', { name: 'E-mail', exact: true })
    const left = await box(name)
    const right = await box(email)
    expect(Math.abs(left.y - right.y)).toBeLessThanOrEqual(1)
    expect(right.x).toBeGreaterThanOrEqual(left.x + left.width)
    expect(right.x + right.width).toBeLessThanOrEqual(viewport.width)
    expect(left.height).toBeGreaterThanOrEqual(44)
    await noHorizontalOverflow(page)
    await preview(page, testInfo, 'tablet-registration')

    await page.getByRole('button', { name: 'Vytvořit účet', exact: true }).tap()
    await expect(page.getByRole('alert')).toBeFocused()
    await noHorizontalOverflow(page)
    await page.getByText('Člen', { exact: true }).tap()
    await expect(page.getByRole('radio', { name: /^Člen/ })).toBeChecked()
    await page.getByText('Vyučující', { exact: true }).tap()
    await expect(page.getByRole('radio', { name: /^Člen/ })).not.toBeChecked()

    await page.getByRole('link', { name: 'Přihlaste se', exact: true }).tap()
    await expect(page.getByRole('heading', { name: 'Přihlášení', exact: true })).toBeVisible()
    await noHorizontalOverflow(page)
    const submit = await box(page.getByRole('button', { name: 'Přihlásit se', exact: true }))
    expect(submit.height).toBeGreaterThanOrEqual(44)
    await preview(page, testInfo, 'tablet-login')
    await page.getByLabel('Heslo', { exact: true }).fill('heslo-pro-tablet-2026')
    await page.getByRole('button', { name: 'Zobrazit heslo', exact: true }).tap()
    await expect(page.getByLabel('Heslo', { exact: true })).toHaveAttribute('type', 'text')
    await page.getByRole('checkbox', { name: /^Zůstat přihlášen/ }).tap()
    await expect(page.getByRole('checkbox', { name: /^Zůstat přihlášen/ })).toBeChecked()
  })
}

for (const type of ['STUDENT', 'TEACHER'] as const) {
  test(`tablet ${type}: registrace, login, otočení a logout dotykem`, async ({ page }, testInfo) => {
    const email = `tablet-${crypto.randomUUID()}@example.test`
    const password = 'heslo-pro-tablet-2026'
    const displayName = 'Tabletový uživatel s dlouhým jménem pro kontrolu hlavičky'
    await page.setViewportSize({ width: 768, height: 1024 })
    await page.goto('/register')
    await page.getByRole('textbox', { name: 'Jméno a příjmení', exact: true }).fill(displayName)
    await page.getByRole('textbox', { name: 'E-mail', exact: true }).fill(email)
    await page.getByLabel(/^Heslo\s*\*?$/u).fill(password)
    await page.getByLabel(/^Heslo znovu\s*\*?$/u).fill(password)
    await page.getByText(type === 'STUDENT' ? 'Člen' : 'Vyučující', { exact: true }).tap()

    // Menší viditelná výška při otevřené softwarové klávesnici musí dovolit posun k odeslání.
    await page.setViewportSize({ width: 768, height: 480 })
    await noHorizontalOverflow(page)
    await page.getByRole('button', { name: 'Vytvořit účet', exact: true }).tap()
    await expect(page).toHaveURL('/login')
    await expect(page.getByLabel('E-mail', { exact: true })).toHaveValue(email)
    await page.getByLabel('Heslo', { exact: true }).fill(password)
    await page.getByRole('button', { name: 'Přihlásit se', exact: true }).tap()
    await expect(page).toHaveURL(type === 'STUDENT' ? '/projects' : '/teaching')

    for (const viewport of [{ width: 768, height: 1024 }, { width: 1024, height: 768 }]) {
      await page.setViewportSize(viewport)
      await expect(page.getByText(displayName, { exact: true })).toBeVisible()
      await noHorizontalOverflow(page)
      const identity = await box(page.getByText(displayName, { exact: true }))
      const logout = await box(page.getByRole('button', { name: 'Odhlásit se', exact: true }))
      expect(identity.x + identity.width).toBeLessThanOrEqual(logout.x)
      expect(logout.height).toBeGreaterThanOrEqual(44)
      expect(logout.x + logout.width).toBeLessThanOrEqual(viewport.width)
      await preview(page, testInfo, `tablet-home-${viewport.width}`)
    }
    await page.getByRole('button', { name: 'Odhlásit se', exact: true }).tap()
    await expect(page).toHaveURL('/login')
    await expect(page.getByRole('status')).toHaveText('Byli jste odhlášeni.')
  })
}
