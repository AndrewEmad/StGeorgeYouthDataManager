import {test,expect} from '@playwright/test';
import {authenticate,fixtures,noOverflow} from './fixtures';

const viewports=[320,360,390,430,768,1024,1440,1920].map(width=>({width,height:900}));
viewports.push({width:768,height:360},{width:1024,height:360});
for(const viewport of viewports)test(`admin navigation fits ${viewport.width}x${viewport.height}`,async({page,context})=>{
  await authenticate(context,'admin');await fixtures(context);await page.setViewportSize(viewport);
  await page.goto('/admin/#/dashboard/tasks');await page.waitForLoadState('networkidle');
  const status=page.locator('.top-bar').getByRole('link',{name:'حالة التطبيق',exact:true});
  await expect(status).toBeVisible();const statusBox=(await status.boundingBox())!;
  expect(statusBox.width).toBeCloseTo(44,2);expect(statusBox.height).toBeCloseTo(44,2);
  const mobile=viewport.width<=768;
  const trigger=page.getByRole('button',{name:'القائمة',exact:true});
  if(mobile)await trigger.click();
  const nav=page.getByRole('navigation',{name:'التنقل الرئيسي'});
  await expect(nav).toBeVisible();await expect(nav.getByRole('link')).toHaveCount(13);
  await expect(nav.getByRole('link',{name:'حالة التطبيق',exact:true})).toHaveCount(0);
  await expect(nav.getByRole('link').nth(0)).toHaveText('analyticsالإحصائيات');
  await expect(nav.getByRole('link').nth(1)).toHaveText('assessmentالتقارير');
  const last=nav.getByRole('link',{name:'تصدير البيانات',exact:true});
  await last.scrollIntoViewIfNeeded();const lastBox=(await last.boundingBox())!;
  expect(lastBox.y).toBeGreaterThanOrEqual(0);expect(lastBox.y+lastBox.height).toBeLessThanOrEqual(viewport.height);
  if(mobile){
    const dialog=page.getByRole('dialog');const drawer=(await dialog.boundingBox())!;expect(drawer.y).toBeCloseTo(0,2);expect(drawer.height).toBeCloseTo(viewport.height,2);expect(drawer.x+drawer.width).toBeCloseTo(viewport.width,2);expect(await dialog.evaluate(e=>getComputedStyle(e).borderRadius)).toBe('0px');const close=dialog.getByRole('button',{name:'إغلاق',exact:true});
    await close.scrollIntoViewIfNeeded();const closeBox=(await close.boundingBox())!;
    expect(closeBox.width).toBeCloseTo(44,2);expect(closeBox.height).toBeCloseTo(44,2);
    await expect(close.locator('.material-icons')).toHaveText('close');await close.click();
    await expect(dialog).not.toBeVisible();await expect(trigger).toBeFocused();
    await trigger.click();await expect(dialog).toBeVisible();await page.keyboard.press('Escape');await expect(dialog).not.toBeVisible();await expect(trigger).toBeFocused();
    await trigger.click();await last.click();await expect(dialog).not.toBeVisible();await expect(page).toHaveURL(/data-exports/);
  }else{
    const sidebar=(await page.locator('.sidebar').boundingBox())!;
    expect(sidebar.width).toBeCloseTo(240,2);expect(sidebar.y).toBeCloseTo(0,2);expect(sidebar.x+sidebar.width).toBeCloseTo(viewport.width,2);expect(sidebar.height).toBeCloseTo(viewport.height,2);expect(await page.locator('.sidebar').evaluate(e=>getComputedStyle(e).borderRadius)).toBe('0px');
    const header=(await page.locator('.top-bar').boundingBox())!;expect(header.x+header.width).toBeCloseTo(viewport.width-240,2);
    expect(sidebar.y+sidebar.height).toBeLessThanOrEqual(viewport.height);
  }
  await status.click();await expect(page.locator('main h1')).toHaveText('حالة التطبيق والإشعارات');
  const overflow=await noOverflow(page);expect(overflow.scroll).toBeLessThanOrEqual(viewport.width+1);
});

test('admin menu remains usable with enlarged Arabic text in landscape',async({page,context})=>{
  await authenticate(context,'admin');await fixtures(context);await page.setViewportSize({width:768,height:360});
  await page.goto('/admin/#/dashboard/tasks');await page.addStyleTag({content:'html{font-size:200% !important}'});
  await page.getByRole('button',{name:'القائمة',exact:true}).click();const dialog=page.getByRole('dialog');
  const last=dialog.getByRole('link',{name:'تصدير البيانات',exact:true});await last.scrollIntoViewIfNeeded();
  const box=(await last.boundingBox())!;expect(box.y+box.height).toBeLessThanOrEqual(360);
  const close=dialog.getByRole('button',{name:'إغلاق',exact:true});await close.scrollIntoViewIfNeeded();await close.click();
  const overflow=await noOverflow(page);expect(overflow.scroll).toBeLessThanOrEqual(769);
});
