import { test, expect, Locator } from '@playwright/test';
import AxeBuilder from '@axe-core/playwright';
import { authenticate, fixtures, noOverflow, longName, id } from './fixtures';

const choices = Array.from({length: 51}, (_, index) => ({
  id: `77777777-7777-7777-7777-${String(index + 1).padStart(12, '0')}`,
  name: `مخدوم ${String(index + 1).padStart(2, '0')}`
}));
const rows = (page: number) => choices.slice((page - 1) * 25, page * 25);
async function bottom(list: Locator) {
  await list.evaluate(element => { element.scrollTop = element.scrollHeight; element.dispatchEvent(new Event('scroll')); });
}

for (const app of ['admin', 'servant'] as const) {
  test(`${app} dropdown appends on scroll without page buttons and saves a later-page choice`, async ({page, context}) => {
    await authenticate(context, app); await fixtures(context);
    const pages: number[] = []; let release!: () => void;
    const gate = new Promise<void>(resolve => release = resolve);
    await context.route('**/api/lookups/students?*', async route => {
      const index = Number(new URL(route.request().url()).searchParams.get('page')); pages.push(index);
      if (index === 2) await gate;
      await route.fulfill({json: {items: rows(index), totalCount: 51}});
    });
    let saved: any;
    await context.route('**/api/follow-up-tasks', route => {
      saved = route.request().postDataJSON(); return route.fulfill({json: {}});
    });
    await page.goto(`/${app}/#/${app === 'admin' ? 'dashboard/' : ''}tasks`);
    await page.getByRole('button', {name: 'مهمة جديدة', exact: true}).click();
    const picker = page.getByRole('dialog').locator('ydm-resource-picker').filter({has: page.getByRole('combobox', {name: 'المخدوم', exact: true})});
    const combo = picker.getByRole('combobox'); await combo.click();
    const list = picker.getByRole('listbox'); await expect(list.getByRole('option')).toHaveCount(25);
    await expect(picker.getByRole('button', {name: /التالي|السابق/})).toHaveCount(0);
    await bottom(list); await expect.poll(() => pages).toEqual([1, 2]);
    for (let index = 0; index < 5; index++) await bottom(list);
    expect(pages).toEqual([1, 2]); await expect(list.getByRole('option')).toHaveCount(25);
    await expect(picker.getByRole('status')).toContainText('جارٍ تحميل'); release();
    await expect(list.getByRole('option')).toHaveCount(50); await bottom(list);
    await expect(list.getByRole('option')).toHaveCount(51); expect(pages).toEqual([1, 2, 3]);
    await bottom(list); await expect(picker.getByRole('status')).toContainText('عرض 51 من 51');
    await list.getByRole('option', {name: choices[50].name, exact: true}).click();
    await expect(list).not.toBeVisible(); await expect(combo).toHaveValue(choices[50].name);
    await combo.click(); await expect(picker.getByRole('option', {selected: true})).toHaveAccessibleName(choices[50].name);
    expect(pages).toEqual([1, 2, 3]); await combo.press('Escape');
    await page.getByRole('dialog').getByRole('button', {name: 'حفظ', exact: true}).click();
    await expect.poll(() => saved?.studentId).toBe(choices[50].id);
  });
}

test('failed additional page retains options; retry reuses its page and deduplicates IDs', async ({page, context}) => {
  await authenticate(context, 'admin'); await fixtures(context);
  const pages: number[] = []; let failed = false;
  await context.route('**/api/lookups/students?*', async route => {
    const index = Number(new URL(route.request().url()).searchParams.get('page')); pages.push(index);
    if (index === 2 && !failed) { failed = true; await route.fulfill({status: 503, json: {message: 'تعذر تحميل المزيد'}}); return; }
    await route.fulfill({json: {items: index === 2 ? [choices[24], ...rows(index)] : rows(index), totalCount: 51}});
  });
  await page.goto('/admin/#/dashboard/tasks'); await page.getByRole('button', {name: 'مهمة جديدة', exact: true}).click();
  const combo = page.getByRole('combobox', {name: 'المخدوم', exact: true}); await combo.click();
  const picker = page.getByRole('dialog').locator('ydm-resource-picker').first(), list = picker.getByRole('listbox');
  await expect(list.getByRole('option')).toHaveCount(25); await bottom(list);
  await expect(picker.getByRole('alert')).toBeVisible(); await expect(list.getByRole('option')).toHaveCount(25);
  await bottom(list); expect(pages).toEqual([1, 2]);
  await picker.getByRole('button', {name: 'إعادة المحاولة'}).click(); await expect(list.getByRole('option')).toHaveCount(50);
  expect(pages).toEqual([1, 2, 2]); await list.getByRole('option', {name: choices[0].name, exact: true}).click();
  await expect(combo).toHaveValue(choices[0].name);
});

test('search resets paging, cancels stale results, retains selection and allows clearing it', async ({page, context}) => {
  await authenticate(context, 'admin'); await fixtures(context);
  const reads: {page: number; search: string}[] = []; let release!: () => void;
  const gate = new Promise<void>(resolve => release = resolve);
  await context.route('**/api/lookups/servants?*', async route => {
    const params = new URL(route.request().url()).searchParams;
    const search = params.get('search') || '', index = Number(params.get('page')); reads.push({page: index, search});
    if (search === 'قديم') await gate;
    const items = search === 'جديد' ? [{id: 'new-person', name: 'نتيجة البحث الجديد'}] : search === 'قديم' ? [{id: 'stale-person', name: 'نتيجة قديمة'}] : rows(index);
    await route.fulfill({json: {items, totalCount: search ? items.length : 51}});
  });
  await page.goto('/admin/#/dashboard/tasks'); await page.getByRole('button', {name: 'مهمة جديدة', exact: true}).click();
  const combo = page.getByRole('combobox', {name: 'المسؤول', exact: true}); await combo.click();
  const picker = page.getByRole('dialog').locator('ydm-resource-picker').last(), list = picker.getByRole('listbox');
  await expect(list.getByRole('option')).toHaveCount(26); await bottom(list); await expect(list.getByRole('option')).toHaveCount(51);
  await list.getByRole('option', {name: choices[30].name, exact: true}).click(); await expect(combo).toHaveValue(choices[30].name);
  await combo.click(); await combo.fill('قديم'); await expect.poll(() => reads.some(read => read.search === 'قديم')).toBe(true);
  await combo.fill('جديد'); await expect(list.getByRole('option', {name: 'نتيجة البحث الجديد', exact: true})).toBeVisible();
  release(); await expect(list.getByRole('option', {name: 'نتيجة قديمة', exact: true})).toHaveCount(0);
  await expect(list.getByRole('option', {name: choices[30].name, exact: true})).toHaveAttribute('aria-selected', 'true');
  await expect(picker).toContainText(`الاختيار الحالي: ${choices[30].name}`);
  expect(reads.filter(read => read.search).map(read => read.page)).toEqual([1, 1]);
  await combo.press('Escape'); await expect(combo).toHaveValue(choices[30].name);
  await combo.click(); await list.getByRole('option', {name: 'غير مسند', exact: true}).click(); await expect(combo).toHaveValue('');
});

test('keyboard paging, reusable participants, Escape and focus stay inside the dialog', async ({page, context}) => {
  await authenticate(context, 'servant'); await fixtures(context);
  const pages: number[] = [];
  await context.route('**/api/lookups/servants?*', route => {
    const index = Number(new URL(route.request().url()).searchParams.get('page')); pages.push(index);
    return route.fulfill({json: {items: rows(index), totalCount: 51}});
  });
  await page.goto('/servant/#/tasks'); const trigger = page.getByRole('button', {name: 'إنجاز وتسجيل النشاط', exact: true}); await trigger.click();
  const dialog = page.getByRole('dialog'), combo = dialog.getByRole('combobox', {name: 'إضافة مشارك', exact: true}); await combo.focus();
  const list = dialog.getByRole('listbox'); await expect(list.getByRole('option')).toHaveCount(25);
  await combo.press('End'); await expect(list.getByRole('option')).toHaveCount(50); expect(pages).toEqual([1, 2]);
  await combo.press('ArrowDown'); await combo.press('Enter');
  await expect(dialog.getByRole('button', {name: choices[25].name + ' ×', exact: true})).toBeVisible(); await expect(combo).toHaveValue('');
  await combo.press('Enter'); await expect(list).toBeVisible(); await combo.press('Home'); await combo.press('Enter');
  await expect(dialog.getByRole('button', {name: choices[0].name + ' ×', exact: true})).toBeVisible();
  await combo.click(); await combo.press('Escape'); await expect(list).not.toBeVisible(); await expect(dialog).toBeVisible(); await expect(combo).toBeFocused();
  await combo.press('Enter'); await expect(list).toHaveAttribute('tabindex', '-1');
  await combo.press('Tab'); await expect(list).not.toBeVisible();
  await expect.poll(() => page.evaluate(() => !!document.activeElement?.closest('dialog[open]'))).toBe(true);
  await page.keyboard.press('Escape'); await expect(dialog).not.toBeVisible(); await expect(trigger).toBeFocused();
});

test('saving locks dropdowns and emits only the chosen IDs', async ({page, context}) => {
  await authenticate(context, 'admin'); await fixtures(context);
  await context.route('**/api/lookups/students?*', route => route.fulfill({json: {items: rows(1), totalCount: 25}}));
  let release!: () => void; const gate = new Promise<void>(resolve => release = resolve); let saved: any;
  await context.route('**/api/follow-up-tasks', async route => { saved = route.request().postDataJSON(); await gate; await route.fulfill({json: {}}); });
  await page.goto('/admin/#/dashboard/tasks'); await page.getByRole('button', {name: 'مهمة جديدة', exact: true}).click();
  const dialog = page.getByRole('dialog'), combo = dialog.getByRole('combobox', {name: 'المخدوم', exact: true}); await combo.click();
  await dialog.getByRole('option', {name: choices[0].name, exact: true}).click();
  await dialog.getByRole('button', {name: 'حفظ', exact: true}).click(); await expect.poll(() => saved?.studentId).toBe(choices[0].id);
  for (const field of await dialog.locator('ydm-resource-picker input, ydm-resource-picker button').all()) await expect(field).toBeDisabled();
  await expect(dialog.getByRole('listbox')).toHaveCount(0); release(); await expect(dialog).not.toBeVisible();
});

test('empty and failed first pages can recover without stale actionable options', async ({page, context}) => {
  await authenticate(context, 'admin'); await fixtures(context);
  let reads = 0;
  await context.route('**/api/lookups/students?*', route => {
    reads++;
    if (reads === 1) return route.fulfill({status: 503, json: {message: 'تعذر تحميل الأسماء'}});
    return route.fulfill({json: {items: [], totalCount: 0}});
  });
  await page.goto('/admin/#/dashboard/tasks'); await page.getByRole('button', {name: 'مهمة جديدة', exact: true}).click();
  const dialog = page.getByRole('dialog'); await dialog.getByRole('combobox', {name: 'المخدوم', exact: true}).click();
  const picker = dialog.locator('ydm-resource-picker').first(); await expect(picker.getByRole('alert')).toBeVisible();
  await expect(picker.getByRole('option')).toHaveCount(0); await picker.getByRole('button', {name: 'إعادة المحاولة'}).click();
  await expect(picker.getByRole('status')).toHaveText('لا توجد نتائج مطابقة.'); expect(reads).toBe(2);
  await bottom(picker.getByRole('listbox')); await expect(picker.getByRole('option')).toHaveCount(0); expect(reads).toBe(2);
});

test('an existing off-page owner remains named and clearing does not expose the unassignment sentinel', async ({page, context}) => {
  await authenticate(context, 'admin'); await fixtures(context);
  await context.route('**/api/lookups/servants?*', route => {
    const params = new URL(route.request().url()).searchParams;
    return route.fulfill({json: {items: params.get('search') ? [] : rows(Number(params.get('page'))), totalCount: params.get('search') ? 0 : 51}});
  });
  let unassigned = 0;
  await context.route(`**/api/StudentCommands/${id}/unassign`, route => { unassigned++; return route.fulfill({json: {}}); });
  await page.goto('/admin/#/dashboard/students'); await page.getByRole('button', {name: 'تخصيص', exact: true}).first().click();
  const dialog = page.getByRole('dialog'), combo = dialog.getByRole('combobox', {name: 'المسؤول', exact: true});
  await expect(combo).toHaveValue(longName); await combo.click();
  await expect(dialog.getByRole('option', {selected: true})).toHaveAccessibleName(longName);
  await combo.fill('غير موجود'); await expect(dialog.getByRole('status')).toHaveText('لا توجد نتائج مطابقة.');
  await expect(dialog.getByRole('option', {selected: true})).toHaveAccessibleName(longName);
  await dialog.getByRole('option', {name: 'غير مسند', exact: true}).click(); await expect(combo).toHaveValue('');
  await combo.click(); await expect(dialog.getByRole('option', {name: longName, exact: true})).toHaveCount(0);
  await expect(dialog.getByRole('option', {selected: true})).toHaveAccessibleName('غير مسند'); await combo.press('Escape');
  await dialog.getByRole('button', {name: 'حفظ (إلغاء التخصيص)', exact: true}).click(); await expect.poll(() => unassigned).toBe(1);
});

test('non-modal report dropdown loads later actors and applies or clears their IDs', async ({page, context}) => {
  await authenticate(context, 'admin'); await fixtures(context);
  const reads: string[] = [];
  await context.route('**/api/lookups/report-actors?*', route => {
    const index = Number(new URL(route.request().url()).searchParams.get('page'));
    return route.fulfill({json: {items: rows(index), totalCount: 51}});
  });
  await context.route('**/api/reports/suite/servant-service-summary?*', route => {
    reads.push(new URL(route.request().url()).searchParams.get('servantId') || ''); return route.fallback();
  });
  await page.goto('/admin/#/dashboard/report-suite?report=servant-service-summary');
  const picker = page.locator('main ydm-resource-picker'), combo = picker.getByRole('combobox', {name: 'الخادم', exact: true});
  await combo.click(); const list = picker.getByRole('listbox'); await expect(list.getByRole('option')).toHaveCount(26);
  await bottom(list); await expect(list.getByRole('option')).toHaveCount(51);
  await list.getByRole('option', {name: choices[30].name, exact: true}).click();
  await page.getByRole('button', {name: 'تطبيق', exact: true}).click(); await expect.poll(() => reads.at(-1)).toBe(choices[30].id);
  await combo.click(); await list.getByRole('option', {name: 'كل الخدام', exact: true}).click();
  await page.getByRole('button', {name: 'تطبيق', exact: true}).click(); await expect.poll(() => reads.at(-1)).toBe('');
});

const sizes = [320, 360, 390, 430, 768, 1024, 1440, 1920].map(width => ({width, height: 844, zoom: false}));
sizes.push({width: 844, height: 390, zoom: false}, {width: 1024, height: 768, zoom: false}, {width: 320, height: 844, zoom: true});
for (const size of sizes) test(`dropdown layout ${size.width}x${size.height}${size.zoom ? ' enlarged text' : ''}`, async ({page, context}, info) => {
  await authenticate(context, 'admin'); await authenticate(context, 'servant'); await fixtures(context); await page.setViewportSize(size);
  await context.route('**/api/lookups/students?*', route => {
    const index = Number(new URL(route.request().url()).searchParams.get('page'));
    return route.fulfill({json: {items: rows(index).map(item => ({...item, name: item.name + ' ' + longName})), totalCount: 51}});
  });
  for (const app of ['admin', 'servant']) {
    await page.goto(`/${app}/#/${app === 'admin' ? 'dashboard/' : ''}tasks`);
    if (size.zoom) await page.addStyleTag({content: 'html {font-size: 200% !important}'});
    await page.getByRole('button', {name: 'مهمة جديدة', exact: true}).click(); const dialog = page.getByRole('dialog');
    await dialog.getByRole('combobox', {name: 'المخدوم', exact: true}).click(); const list = dialog.getByRole('listbox');
    await expect(list.getByRole('option')).toHaveCount(25); await bottom(list);
    await expect.poll(() => list.getByRole('option').count()).toBeGreaterThanOrEqual(50);
    expect((await noOverflow(page)).scroll).toBeLessThanOrEqual(size.width + 1);
    const bounds = await list.boundingBox(); expect(bounds!.width).toBeLessThanOrEqual(size.width); expect(bounds!.height).toBeLessThanOrEqual(size.height * .35 + 1);
    expect(await list.evaluate(element => Array.from(element.children).every(option => option.scrollWidth <= option.clientWidth + 1))).toBe(true);
    if ((size.width === 390 || size.width === 1440) && !size.zoom) {
      const result = await new AxeBuilder({page}).include('dialog[open]').withTags(['wcag2a', 'wcag2aa', 'wcag21aa']).analyze();
      expect(result.violations.map(issue => ({id: issue.id, nodes: issue.nodes.map(node => node.target)}))).toEqual([]);
    }
    await page.screenshot({path: info.outputPath(`${app}-dropdown.png`)});
    await list.getByRole('option').last().click(); await expect(list).not.toBeVisible();
    await expect(dialog.getByRole('button', {name: 'حفظ', exact: true})).toBeVisible();
  }
});
