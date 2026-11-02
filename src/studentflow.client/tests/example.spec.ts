
import { test, expect } from '@playwright/test';

test('nepřihlášený uživatel pokračuje na přihlášení bez soukromého obsahu', async ({ page }) => {
  await page.goto('/');

  await expect(
    page.getByRole('heading', { name: 'StudentFlow' })
  ).toBeVisible();
  await expect(page).toHaveURL('/login');
  await expect(page.getByRole('heading', { name: 'Přihlášení', exact: true })).toBeVisible();
  await expect(page.getByRole('heading', { name: 'Kanban', exact: true })).toHaveCount(0);
  await expect(page.getByRole('alert')).toHaveCount(0);
});

test('API je dostupné ze stejného originu jako frontend', async ({ request }) => {
  const response = await request.get('/api/health');

  expect(response.status()).toBe(200);
  expect(response.headers()['content-type']).toContain('application/json');
  expect(await response.json()).toEqual({ status: 'ok' });
});
