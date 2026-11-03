import { test, expect } from '@playwright/test';

test('zobrazi dashboard Moje projekty', async ({ page }) => {
  await page.goto('/');

  await expect(
    page.getByRole('heading', { name: 'Moje projekty' })
  ).toBeVisible();
});