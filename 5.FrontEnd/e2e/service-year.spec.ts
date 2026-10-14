import {test,expect} from '@playwright/test';
import {authenticate,fixtures,noOverflow,id} from './fixtures';

for(const app of ['admin','servant'] as const){
 const root=app==='admin' ? '/dashboard' : '';
 test(`${app} year applies to routes exports print and survives navigation`,async({page,context})=>{
  await authenticate(context,app);await fixtures(context);
  const requests:URL[]=[];page.on('request',r=>{if(r.method()==='GET' && r.url().includes('/api/'))requests.push(new URL(r.url()));});
  await page.goto(`/${app}/#${root}/report-suite?report=attendance`);
  const year=page.getByRole('combobox',{name:'عام الخدمة',exact:true});await expect(year).toHaveValue('');
  await year.selectOption('2026');expect(await page.evaluate(()=>Object.keys(localStorage).filter(k=>k.includes('.service-year.')).map(k=>localStorage.getItem(k)))).toContain('2026');await expect(page.locator('input[name="from"]')).toHaveValue('2026-09-18');await expect(page.locator('input[name="to"]')).toHaveValue('2027-09-10');
  await expect.poll(()=>requests.some(u=>u.pathname.endsWith('/reports/suite/attendance') && u.searchParams.get('serviceYear')==='2026')).toBe(true);
  const download=page.waitForEvent('download');await page.getByRole('button',{name:'CSV · كل النتائج',exact:true}).click();await download;
  await expect.poll(()=>requests.some(u=>u.pathname.endsWith('/export') && u.searchParams.get('serviceYear')==='2026')).toBe(true);
  await page.evaluate(()=>{window.print=()=>{};});await page.getByRole('button',{name:'طباعة كل النتائج',exact:true}).click();
  await expect.poll(()=>requests.some(u=>u.searchParams.get('full')==='true' && u.searchParams.get('serviceYear')==='2026')).toBe(true);
  // Local report periods intersect the global year and survive reload unchanged.
  await page.goto(`/${app}/#${root}/report-suite?report=attendance&dateFrom=2026-10-01&dateTo=2026-10-31`);
  await year.selectOption('2025');await expect(page.locator('input[name="from"]')).toHaveValue('2026-10-01');
  await expect.poll(()=>requests.some(u=>u.searchParams.get('serviceYear')==='2025' && u.searchParams.get('dateFrom')==='2026-10-01')).toBe(true);
  await page.reload();await expect(year).toHaveValue('2025');await expect(page.locator('input[name="to"]')).toHaveValue('2026-10-31');await year.selectOption('2026');
  for(const [route,endpoint] of [['requests','/approvals/all'],['tasks','/follow-up-tasks'],['activity-history','/activity-history/calls'],['attendance','/meetings'],[app==='admin' ? 'stats' : 'dashboard',app==='admin' ? '/Reports/dashboard/manager' : '/Reports/dashboard/servant']] as const){
   requests.length=0;await page.goto(`/${app}/#${root}/${route}`);await expect(year).toHaveValue('2026');
   await expect.poll(()=>requests.some(u=>u.pathname.toLowerCase().endsWith(endpoint.toLowerCase()) && u.searchParams.get('serviceYear')==='2026')).toBe(true);
  }
  await page.reload();expect(await page.evaluate(()=>Object.keys(localStorage).filter(k=>k.includes('.service-year.')).map(k=>localStorage.getItem(k)))).toContain('2026');await expect(year).toHaveValue('2026');requests.length=0;await year.selectOption('');await expect(year).toHaveValue('');
  await expect.poll(()=>requests.some(u=>u.pathname.toLowerCase().includes('/reports/dashboard/') && u.searchParams.get('serviceYear')==='all')).toBe(true);
 });
 test(`${app} same app tabs preserve an open draft when the year changes`,async({page,context})=>{
  await authenticate(context,app);await fixtures(context);const other=await context.newPage();await other.goto(`/${app}/#${root}/${app==='admin' ? 'stats' : 'dashboard'}`);
  await page.goto(`/${app}/#${root}/students/${id}`);await page.getByRole('button',{name:'تسجيل مكالمة',exact:true}).click();
  const dialog=page.getByRole('dialog');const notes=dialog.locator('textarea').first();await notes.fill('ملاحظات لم تُحفظ بعد');
  await other.getByRole('combobox',{name:'عام الخدمة',exact:true}).selectOption('2025');await expect(page.getByRole('combobox',{name:'عام الخدمة',exact:true})).toHaveValue('2025');await expect(dialog).toBeVisible();await expect(notes).toHaveValue('ملاحظات لم تُحفظ بعد');
  await other.getByRole('combobox',{name:'عام الخدمة',exact:true}).selectOption('');await expect(notes).toHaveValue('ملاحظات لم تُحفظ بعد');await other.close();
 });
}

const sizes=[...[320,360,390,430,768,1024,1440,1920].map(width=>({width,height:900,zoom:false})),{width:844,height:390,zoom:false},{width:320,height:900,zoom:true}];
for(const size of sizes)test(`year header stays accessible at ${size.width}x${size.height}${size.zoom ? ' reflow' : ''}`,async({page,context},info)=>{
 await authenticate(context,'admin');await authenticate(context,'servant');await fixtures(context);await page.setViewportSize(size);
 for(const app of ['admin','servant'] as const){await page.goto(`/${app}/#${app==='admin' ? '/dashboard/stats' : '/dashboard'}`);if(size.zoom)await page.addStyleTag({content:'html{font-size:200% !important}'});
  const year=page.getByRole('combobox',{name:'عام الخدمة',exact:true});await expect(year).toBeVisible();await expect(year).toBeInViewport();await expect(year).toHaveAttribute('dir','ltr');await year.selectOption('2026');await expect(year).toHaveValue('2026');
  const rect=await year.boundingBox();expect(rect!.height).toBeGreaterThanOrEqual(44);expect(rect!.x).toBeGreaterThanOrEqual(0);expect(rect!.x+rect!.width).toBeLessThanOrEqual(size.width+1);
  expect((await noOverflow(page)).scroll).toBeLessThanOrEqual(size.width+1);
  const geometry=await page.evaluate(()=>{const header=document.querySelector('header.top-bar,header.layout-header')!.getBoundingClientRect(),main=document.querySelector('main')!.getBoundingClientRect();return {headerBottom:header.bottom,mainTop:main.top,firstTop:Array.from(document.querySelectorAll('main h1, main p')).find(e=>e.getBoundingClientRect().height>0)?.getBoundingClientRect().top};});
  expect(geometry.firstTop ?? geometry.mainTop).toBeGreaterThanOrEqual(geometry.headerBottom);
  if(size.width===390 || size.width===1440)await page.screenshot({path:info.outputPath(`${app}-year-header.png`)});
 }
});
