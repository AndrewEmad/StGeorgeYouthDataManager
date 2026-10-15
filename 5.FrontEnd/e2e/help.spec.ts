import {test,expect} from '@playwright/test';
import AxeBuilder from '@axe-core/playwright';
import {existsSync,readFileSync,readdirSync} from 'node:fs';
import {authenticate,fixtures,noOverflow} from './fixtures';
import {topics as adminTopics} from '../shared/help/admin';
import {topics as servantTopics} from '../shared/help/servant';
import {reportDefinitions,servantReportDefinitions} from '../shared/core/report-catalog';

for(const app of ['admin','servant']){
 const topics=app==='admin' ? adminTopics : servantTopics;
 const home=app==='admin' ? '/admin/#/dashboard/stats' : '/servant/#/dashboard';
 const guide=app==='admin' ? '/admin/#/dashboard/help' : '/servant/#/help';
 test(`${app} manual covers every page report and illustrated operation`,async({page,context})=>{
  await authenticate(context,app,{role:app==='admin' ? 'Manager' : 'Servant'});await fixtures(context);
  const expectedReports=app==='admin' ? reportDefinitions : servantReportDefinitions;
  for(const r of expectedReports){const t=topics.find(t=>t.report===r.key)!;expect(t,`${r.key} documentation`).toBeTruthy();expect(t.calculation!.length).toBeGreaterThan(1);}
  expect(new Set(topics.map(t=>t.id)).size).toBe(topics.length);
  const required=app==='admin' ? ['stats','reports','students','servants','archived-students','attendance','tasks','requests','activity-history','profile','notifications','operations','data-exports','reports/student-distribution','reports/servant-activity','reports/students-no-contact'] : ['dashboard','students','attendance','tasks','requests','request-add-student','activity-history','profile','notifications','report-suite'];
  for(const path of required)expect(topics.some(t=>t.path===path),path+' has a documented purpose and steps').toBeTruthy();
  const manifest=JSON.parse(readFileSync(`shared/help/assets/${app}/capture-manifest.json`,'utf8'));
  for(const t of topics)for(const p of t.pictures){expect(existsSync(`shared/help/assets/${app}/${p.file}.png`),p.file).toBeTruthy();expect(manifest.captures[p.file]).toBeTruthy();}
  await page.goto(home);const icon=page.locator('header').getByRole('link',{name:'دليل الاستخدام',exact:true});await expect(icon).toBeVisible();await icon.click();await expect(page).toHaveURL(new RegExp('/help'));
  await expect(page.locator('.help-index-link')).toHaveCount(topics.length);
  const failed:string[]=[];page.on('response',r=>{if(r.url().includes('/assets/help/') && !r.ok())failed.push(r.url());});
  for(const t of topics){
   await page.locator('.help-index').getByRole('link',{name:t.title,exact:true}).click();
   await expect(page.locator('.help-title')).toHaveText(t.title);
   for(const img of await page.locator('.help-shot img').all()){await img.scrollIntoViewIfNeeded();await expect.poll(()=>img.evaluate((e:HTMLImageElement)=>e.complete && e.naturalWidth>0)).toBeTruthy();await expect(img).toHaveAttribute('alt',/.+/);}
  }
  expect(failed).toEqual([]);
  if(app==='servant'){await expect(page.locator('.help-index-link').filter({hasText:'إدارة الخدام والحسابات'})).toHaveCount(0);await page.goto(guide+'?section=attendance-duty');await expect(page.locator('.help-open')).toHaveCount(0);}
 });

 test(`${app} manual search bookmarks keyboard and full illustrated printing work`,async({page,context})=>{
  await authenticate(context,app);await fixtures(context);await page.addInitScript(()=>{window.print=()=>{(window as any).__manualPrinted=((window as any).__manualPrinted || 0)+1;};});
  await page.goto(guide+'?section=report-contact');await expect(page.locator('.help-title')).toHaveText(app==='admin' ? 'الافتقاد والتواصل' : 'افتقادي لمجموعتي');
  await page.getByRole('searchbox',{name:'ابحث في الدليل'}).fill('كلمة ليست موجودة إطلاقا');await expect(page.getByRole('status').filter({hasText:'لا توجد أجزاء'})).toBeVisible();
  await page.getByRole('button',{name:'مسح البحث',exact:true}).click();await page.getByRole('searchbox',{name:'ابحث في الدليل'}).fill('نسبة الحضور');await expect(page.locator('.help-index-link').first()).toBeVisible();
  await page.getByRole('button',{name:'مسح البحث',exact:true}).click();await page.locator('.help-index-link').filter({hasText:'تسجيل الدخول والخروج'}).click();await expect(page.locator('.help-title')).toBeFocused();await page.reload();await expect(page.locator('.help-title')).toHaveText('تسجيل الدخول والخروج');
  await page.getByRole('button',{name:'طباعة الدليل كاملًا',exact:true}).click();await expect.poll(()=>page.evaluate(()=>(window as any).__manualPrinted || 0),{timeout:20000}).toBe(1);
  await expect(page.locator('.help-article')).toHaveCount(topics.length);expect(await page.locator('.help-shot img').evaluateAll(imgs=>imgs.every((img:any)=>img.complete && img.naturalWidth>0))).toBeTruthy();
  await page.emulateMedia({media:'print'});await expect(page.locator('.help-index')).not.toBeVisible();await expect(page.locator('.help-print-index')).toBeVisible();await expect(page.locator('header').first()).not.toBeVisible();
  await page.emulateMedia({media:'screen'});await page.evaluate(()=>dispatchEvent(new Event('afterprint')));await expect(page.locator('.help-article')).toHaveCount(1);await expect(page.locator('.help-title')).toHaveText('تسجيل الدخول والخروج');
  await page.getByRole('button',{name:'طباعة هذا الجزء',exact:true}).click();await expect.poll(()=>page.evaluate(()=>(window as any).__manualPrinted || 0)).toBe(2);await expect(page.locator('.help-article')).toHaveCount(1);await expect(page.locator('.help-print-index')).toHaveCount(0);await page.evaluate(()=>dispatchEvent(new Event('afterprint')));
 });

 test(`${app} missing manual images have a readable error and do not print incomplete instructions`,async({page,context})=>{
  await authenticate(context,app);await fixtures(context);await page.addInitScript(()=>{window.print=()=>{(window as any).__manualPrinted=true;};});
  await context.route('**/assets/help/home.png',r=>r.abort());await page.goto(guide);await page.locator('.help-shot img').first().scrollIntoViewIfNeeded();await expect(page.getByRole('alert')).toContainText('تعذر تحميل صورة');
  await page.getByRole('button',{name:'طباعة الدليل كاملًا',exact:true}).click();await expect(page.getByRole('alert')).toContainText('لم تكتمل صور الدليل');expect(await page.evaluate(()=>(window as any).__manualPrinted || false)).toBeFalsy();await expect(page.locator('.help-title')).toBeVisible();
 });

 test(`${app} failed guide chapter download ends loading with a readable recovery message`,async({page,context})=>{
  await authenticate(context,app);await fixtures(context);
  const dir=`${app==='admin' ? 'AdminApp' : 'ServantApp'}/dist/${app==='admin' ? 'AdminApp' : 'ServantApp'}/browser`;
  const marker=app==='admin' ? 'إدارة الخدام والحسابات والأدوار' : 'خدمتي: مجموعتي وافتقادي خلال الأسبوع';
  const chunk=readdirSync(dir).find(file=>file.endsWith('.js') && readFileSync(dir+'/'+file,'utf8').replace(/\\u([\da-fA-F]{4})/g,(_,hex)=>String.fromCharCode(parseInt(hex,16))).includes(marker));expect(chunk).toBeTruthy();
  await context.route('**/'+chunk,r=>r.abort());await page.goto(guide);await expect(page.getByRole('alert')).toContainText('تعذر فتح الدليل');await expect(page.getByText('جارٍ فتح الدليل…',{exact:true})).not.toBeVisible();await expect(page.getByRole('button',{name:'طباعة الدليل كاملًا',exact:true})).toBeDisabled();
 });

 test(`${app} manual links and screenshots resolve under a deployed subpath`,async({page,context})=>{
  await authenticate(context,app);await fixtures(context);
  await context.route(`**/church/youth/${app}/**`,async route=>{
   const url=route.request().url().replace(`/church/youth/${app}/`,`/${app}/`),response=await route.fetch({url});
   if(response.headers()['content-type']?.includes('text/html'))return route.fulfill({response,body:(await response.text()).replace(`<base href="/${app}/">`,`<base href="/church/youth/${app}/">`)});
   return route.fulfill({response});
  });
  await page.goto(guide.replace(`/${app}/`,`/church/youth/${app}/`));await expect(page.locator('.help-title')).toBeVisible();const image=page.locator('.help-shot img').first();await image.scrollIntoViewIfNeeded();await expect.poll(()=>image.evaluate((e:HTMLImageElement)=>e.naturalWidth)).toBeGreaterThan(0);expect(await image.getAttribute('src')).toContain(`/church/youth/${app}/assets/help/`);
 });

 for(const viewport of [...[320,360,390,430,768,1024,1440,1920].map(width=>({width,height:900})),{width:768,height:360},{width:1024,height:360}])test(`${app} help header and manual fit ${viewport.width}x${viewport.height}`,async({page,context},info)=>{
  await authenticate(context,app);await fixtures(context);await page.setViewportSize(viewport);await page.goto(home);const icon=page.locator('header').getByRole('link',{name:'دليل الاستخدام',exact:true});await expect(icon).toBeVisible();await expect(icon).toBeInViewport();expect((await noOverflow(page)).scroll).toBeLessThanOrEqual(viewport.width+1);await icon.click();await expect(page.locator('.help-title')).toBeVisible();
  await page.locator('.help-index-link').filter({hasText:app==='admin' ? 'ملخص خدمة الخدام' : 'افتقادي لمجموعتي'}).click();await expect(page.locator('.help-calculation')).toBeVisible();expect((await noOverflow(page)).scroll).toBeLessThanOrEqual(viewport.width+1);
  if(viewport.width===320){await page.addStyleTag({content:'html{font-size:200% !important}'});expect((await noOverflow(page)).scroll).toBeLessThanOrEqual(321);}
  if(viewport.width===390 || viewport.width===1440){for(const img of await page.locator('.help-shot img').all()){await img.scrollIntoViewIfNeeded();await img.evaluate((e:HTMLImageElement)=>e.decode());}await page.evaluate(()=>scrollTo(0,0));await page.screenshot({path:info.outputPath(`${app}-help-${viewport.width}.png`),fullPage:true});}
  if(viewport.width===390){expect((await new AxeBuilder({page}).withTags(['wcag2a','wcag2aa','wcag21aa']).analyze()).violations).toEqual([]);}
 });
}
