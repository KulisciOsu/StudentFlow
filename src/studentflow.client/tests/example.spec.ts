
import { test, expect } from '@playwright/test';

test('zobrazi hlavni stranku StudentFlow', async ({ page }) => {
  await page.goto('/');

  await expect(
    page.getByRole('heading', { name: 'StudentFlow' })
  ).toBeVisible();
});