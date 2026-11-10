
import { expect, test } from '@playwright/test'
import type { Page } from '@playwright/test'

// Izolovaná integrace routování a formuláře KAN-86 s autentizací KAN-46.
// Odpovědi identity jsou řízené; skutečné session/API ověřuje session.spec.ts.
async function mockSession(page: Page, accountType: 'STUDENT' | 'TEACHER') {
  let authenticated = true

  await page.route('**/api/auth/me', route => route.fulfill(authenticated ? {
    json: {
      user: {
        id: '1',
        displayName: 'Přihlášený uživatel',
        email: 'integration@example.test',
        accountType,
      },
      session: {
        expiresAt: new Date(Date.now() + 3_600_000).toISOString(),
        persistent: false,
      },
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

// Testovací data pro dashboard, založení projektu a jeho nástěnku.
async function mockStudentApi(page: Page) {
  await page.route('**/api/dashboard', route => route.fulfill({
    json: {
      projects: [
        {
          id: 1,
          name: 'Testovací projekt',
          team: 'Testovací tým',
          membersCount: 3,
          role: 'Člen',
          completed: 2,
          total: 5,
          openTasks: 3,
          overdueTasks: 0,
          milestone: 'První milník',
          members: ['Přihlášený uživatel'],
        },
      ],
      tasks: [
        {
          id: 1,
          name: 'Čištění datasetu',
          project: 'Testovací projekt',
          deadline: '2099-01-15',
          priority: 'medium',
          status: 'Probíhá',
          overdue: false,
          completed: false,
        },
        {
          id: 2,
          name: 'Analýza požadavků',
          project: 'Testovací projekt',
          deadline: '2099-01-15',
          priority: 'high',
          status: 'Hotovo',
          overdue: false,
          completed: true,
        },
        {
          id: 3,
          name: 'Příprava dokumentace',
          project: 'Testovací projekt',
          deadline: '2099-01-20',
          priority: 'low',
          status: 'K provedení',
          overdue: false,
          completed: false,
        },
        {
          id: 4,
          name: 'Kontrola výsledků',
          project: 'Testovací projekt',
          deadline: '2099-01-25',
          priority: 'medium',
          status: 'K provedení',
          overdue: false,
          completed: false,
        },
      ],
    },
  }))

  await page.route('**/api/projects', async route => {
    if (route.request().method() !== 'POST') {
      await route.continue()
      return
    }

    expect(route.request().headers()['x-csrf-token']).toBe('integration-csrf')

    const body = route.request().postDataJSON()

    expect(body).toMatchObject({
      name: 'Integrační projekt',
      teamName: 'Integrační tým',
    })

    await route.fulfill({
      status: 201,
      json: { projectId: 42 },
    })
  })

  await page.route('**/api/projects/42', route => route.fulfill({
    json: {
      id: 42,
      name: 'Integrační projekt',
      description: '',
      teamName: 'Integrační tým',
      dueDate: '2099-01-15',
    },
  }))

  await page.route('**/api/projects/42/columns', route => route.fulfill({
    json: [
      { id: 1, title: 'K provedení', isDone: false },
      { id: 2, title: 'Probíhá', isDone: false },
      { id: 3, title: 'Hotovo', isDone: true },
    ],
  }))

  await page.route('**/api/projects/42/tasks', route => route.fulfill({
    json: [
      {
        id: 1,
        title: 'První integrační úkol',
        columnId: 1,
        position: 0,
        priority: 'medium',
        assignee: 'Přihlášený uživatel',
        dueDate: '2099-01-15',
      },
    ],
  }))
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
  const gate = new Promise<void>(resolve => {
    release = resolve
  })

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
    await mockStudentApi(page)

    await page.goto('/projects')

    await expect(page.getByRole('heading', { name: 'Moje projekty', exact: true })).toBeVisible()
    await expect(page.getByText('Přihlášený uživatel', { exact: true }).first()).toBeVisible()
    await expect(page.getByText('Petra Svobodová', { exact: true })).toHaveCount(0)
    await expect(page.getByRole('main')).toHaveCount(1)
    await expect(page.getByRole('button', { name: 'Odhlásit se', exact: true })).toHaveCount(1)
    expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true)

    await page.getByRole('button', { name: 'Založit projekt', exact: true }).click()

    await expect(page).toHaveURL('/projects/new')
    await expect(page.getByRole('heading', { name: 'Založit projekt', exact: true })).toBeVisible()
    await expect(page.getByText('Přihlášený uživatel', { exact: true }).first()).toBeVisible()
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

    // Po úspěšném založení aplikace přesměruje uživatele na nástěnku.
    await expect(page).toHaveURL('/board/42')
    await expect(page.getByRole('heading', { name: 'Nástěnka', exact: true })).toBeVisible()
    await expect(page.getByText('Integrační projekt · Integrační tým')).toBeVisible()

    await page.screenshot({
      path: testInfo.outputPath('project-form.png'),
      fullPage: true,
    })

    await page.getByRole('link', { name: 'Můj přehled', exact: true }).click()

    await expect(page).toHaveURL('/projects')
    await expect(page.getByRole('heading', { name: 'Moje projekty', exact: true })).toBeVisible()

    await page.goBack()

    await expect(page).toHaveURL('/board/42')
    await expect(page.getByRole('heading', { name: 'Nástěnka', exact: true })).toBeVisible()

    await page.reload()

    await expect(page.getByText('Přihlášený uživatel', { exact: true }).first()).toBeVisible()
    await expect(page.getByRole('heading', { name: 'Nástěnka', exact: true })).toBeVisible()

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

test('dashboard člena zachová filtry úkolů a oba vstupy do založení projektu', async ({ page }) => {
  await mockSession(page, 'STUDENT')
  await mockStudentApi(page)

  await page.goto('/')

  await expect(page).toHaveURL('/projects')
  await expect(page.getByRole('heading', { name: 'Moje projekty', exact: true })).toBeVisible()

  const tasks = page.getByRole('table')

  await expect(tasks.getByRole('button', { name: 'Čištění datasetu', exact: true })).toBeVisible()
  await expect(tasks.getByRole('button', { name: 'Analýza požadavků', exact: true })).toHaveCount(0)

  await page.getByRole('button', { name: /^Hotové/ }).click()

  await expect(tasks.getByRole('button', { name: 'Analýza požadavků', exact: true })).toBeVisible()
  await expect(tasks.getByRole('button', { name: 'Čištění datasetu', exact: true })).toHaveCount(0)

  await page.getByRole('button', { name: 'Otevřené (3)', exact: true }).click()

  await expect(tasks.getByRole('button', { name: 'Čištění datasetu', exact: true })).toBeVisible()

  await page.getByRole('button', { name: /Založit nový projekt/ }).click()

  await expect(page).toHaveURL('/projects/new')

  await page.getByRole('link', { name: 'Můj přehled', exact: true }).click()

  await expect(page).toHaveURL('/projects')

  await page.getByRole('button', { name: 'Odhlásit se', exact: true }).click()

  await expect(page).toHaveURL('/login')
  await expect(page.getByRole('heading', { name: 'Moje projekty', exact: true })).toHaveCount(0)

  await page.goto('/projects')

  await expect(page).toHaveURL('/login')
  await expect(page.getByRole('heading', { name: 'Moje projekty', exact: true })).toHaveCount(0)
})

test('výpadek ověření identity neotevře formulář projektu', async ({ page }) => {
  await page.route('**/api/auth/me', route => route.fulfill({ status: 503 }))

  await page.goto('/projects/new')

  await expect(page.getByRole('heading', { name: 'Přihlášení se nepodařilo ověřit' })).toBeVisible()
  await expect(page.getByLabel('Název projektu')).toHaveCount(0)
})