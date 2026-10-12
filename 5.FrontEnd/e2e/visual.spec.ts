import {test,expect} from '@playwright/test';
import {authenticate,fixtures} from './fixtures';
for(const app of ['admin','servant'])for(const width of [390,1440]){
 for(const state of ['populated','empty','failed','loading'] as const)test(`${app} report visual ${state} at ${width}px`,async({page,context})=>{
  await authenticate(context,app);await fixtures(context,state==='loading' ? 'populated' : state);
  let release:()=>void=()=>{};if(state==='loading'){const gate=new Promise<void>(r=>release=r);await context.route('**/api/reports/suite/**',async route=>{await gate;await route.fallback();});}
  await page.setViewportSize({width,height:900});await page.clock.setFixedTime(new Date('2026-10-11T12:00:00Z'));
  await page.goto(`/${app}/#/${app==='admin' ? 'dashboard/' : ''}report-suite`);await expect(page.locator('main')).toBeVisible();
  if(state==='loading')await expect(page.getByText('جارٍ تحميل النتائج…')).toBeVisible();
  else if(state==='failed')await expect(page.getByRole('alert').first()).toBeVisible();
  else await expect(page.getByText('جارٍ تحميل النتائج…')).not.toBeVisible();
  await page.evaluate(async()=>{await document.fonts.ready;});
  await page.evaluate(()=>window.scrollTo(0,0));await expect(page).toHaveScreenshot(`${app}-report-${width}-${state}.png`,{animations:'disabled',fullPage:true,maxDiffPixelRatio:.015});release();
 });
 test(`${app} dialog visual validation and saving at ${width}px`,async({page,context})=>{
  await authenticate(context,app);await fixtures(context);await page.setViewportSize({width,height:900});await page.goto(`/${app}/#/${app==='admin' ? 'dashboard/' : ''}notifications`);
  await page.getByRole('button',{name:'جدول جديد'}).click();const dialog=page.getByRole('dialog');await dialog.getByRole('button',{name:'حفظ',exact:true}).click();await expect(dialog.getByRole('alert')).toBeVisible();await page.evaluate(async()=>{await document.fonts.ready;});
  await expect(dialog).toHaveScreenshot(`${app}-dialog-${width}-validation.png`,{animations:'disabled',maxDiffPixelRatio:.015});
  await dialog.getByLabel('الجمعة',{exact:true}).check();let release!:()=>void;const gate=new Promise<void>(r=>release=r);await context.route('**/api/Notification/schedules',async route=>{if(route.request().method()!=='POST'){await route.fallback();return;}await gate;await route.fulfill({json:{}});});
  await dialog.getByRole('button',{name:'حفظ',exact:true}).click();await expect(dialog.getByLabel('وقت التذكير')).toBeDisabled();await expect(dialog.getByRole('alert')).toHaveCount(0);
  await expect(dialog).toHaveScreenshot(`${app}-dialog-${width}-saving-disabled.png`,{animations:'disabled',fullPage:true,maxDiffPixelRatio:.015});release();await expect(dialog).not.toBeVisible();
 });
}
