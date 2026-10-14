import { test, expect } from '@playwright/test';
import { authenticate, fixtures, noOverflow } from './fixtures';

const sizes = [...[320, 360, 390, 430, 768, 1024, 1440, 1920].map(width => ({width, height: 900, zoom: false})),
  {width: 844, height: 390, zoom: false}, {width: 1024, height: 768, zoom: false}, {width: 320, height: 900, zoom: true}];

for (const size of sizes) test(`filter actions stay adjacent at ${size.width}x${size.height}${size.zoom ? ' enlarged text' : ''}`, async ({page, context}, info) => {
  await authenticate(context, 'admin'); await authenticate(context, 'servant'); await fixtures(context); await page.setViewportSize(size);
  const routes = [
    '/admin/#/dashboard/report-suite',
    '/admin/#/dashboard/reports/servant-activity',
    '/servant/#/students',
    ...(size.width === 390 ? ['/admin/#/dashboard/requests', '/servant/#/requests', '/admin/#/dashboard/activity-history', '/servant/#/activity-history'] : [])
  ];
  for (const [index, route] of routes.entries()) {
    await page.goto(route); if (size.zoom) await page.addStyleTag({content: 'html{font-size:200% !important}'});
    if (route.includes('reports/servant-activity')) await page.getByRole('button', {name: 'بحث متقدم', exact: true}).click();
    const actions = page.locator('main .ydm-filter-actions').first(); await expect(actions).toBeVisible();
    const apply = actions.getByRole('button', {name: 'تطبيق', exact: true}), clear = actions.getByRole('button', {name: 'مسح', exact: true});
    await expect(apply).toBeVisible(); await expect(clear).toBeVisible();
    await page.evaluate(() => document.fonts.ready); await actions.scrollIntoViewIfNeeded();
    // Read both rectangles in one frame; WebKit may continue scrolling between two locator calls.
    const [a, b] = await actions.evaluate(element => Array.from(element.querySelectorAll('button')).map(button => {
      const {x, y, width, height} = button.getBoundingClientRect(); return {x, y, width, height};
    }));
    expect(Math.abs(a.y - b.y)).toBeLessThanOrEqual(1); expect(Math.abs(a.height - b.height)).toBeLessThanOrEqual(1);
    expect(a.height).toBeGreaterThanOrEqual(44); expect(b.height).toBeGreaterThanOrEqual(44);
    expect(a.x).toBeGreaterThanOrEqual(b.x + b.width); expect(a.x - b.x - b.width).toBeLessThanOrEqual(9);
    expect((await noOverflow(page)).scroll).toBeLessThanOrEqual(size.width + 1);
    await expect(apply).toBeInViewport(); await expect(clear).toBeInViewport();
    if (size.width === 390 || size.width === 1440) await page.screenshot({path: info.outputPath(`filter-actions-${index}.png`)});
  }
});
