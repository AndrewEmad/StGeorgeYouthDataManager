import {test,expect} from '@playwright/test';
import AxeBuilder from '@axe-core/playwright';
import {authenticate,fixtures,owner,noOverflow} from './fixtures';

for(const viewport of [...[320,360,390,430,768,1024,1440,1920].map(width=>({width,height:900})),{width:768,height:360},{width:1024,height:360}])test(`consistent page gutters at ${viewport.width}x${viewport.height}`,async({page,context},info)=>{
 for(const app of ['admin','servant']){
  await authenticate(context,app);await fixtures(context);await page.setViewportSize(viewport);
  const routes=app==='admin' ? ['stats','servants','students','reports','reports/servant-activity','report-suite','activity-history'] : ['dashboard','students','report-suite','activity-history','request-add-student'];
  for(const route of routes){
   await page.goto(`/${app}/#/${app==='admin' ? 'dashboard/' : ''}${route}`);await page.waitForLoadState('networkidle');
   const geometry=await page.locator('main').evaluate(main=>{
    const gutter=Math.min(24,Math.max(12,innerWidth*.02));const style=getComputedStyle(main),box=main.getBoundingClientRect();
    const root=main.querySelector('.ydm-page,.animate-slide,.container,.request-add-student-container')!;const content=root.getBoundingClientRect(),rs=getComputedStyle(root);
    return {gutter,left:Number.parseFloat(style.paddingLeft),right:Number.parseFloat(style.paddingRight),rootLeft:content.left-box.left,rootRight:box.right-content.right,rootPadding:Number.parseFloat(rs.paddingLeft),rootTop:content.top-box.top,top:Number.parseFloat(style.paddingTop)};
   });
   expect(geometry.left,`${app}/${route}`).toBeCloseTo(geometry.gutter,0);expect(geometry.right).toBeCloseTo(geometry.gutter,0);
   expect(geometry.rootLeft,`${app}/${route}: nested padding`).toBeCloseTo(geometry.gutter,0);expect(geometry.rootRight).toBeCloseTo(geometry.gutter,0);expect(geometry.rootPadding).toBe(0);
   expect(geometry.rootTop,`${app}/${route}: top spacing`).toBeCloseTo(geometry.top,0);
   expect((await noOverflow(page)).scroll).toBeLessThanOrEqual(viewport.width+1);
  }
 }
 if(viewport.width===320){await page.addStyleTag({content:'html{font-size:200% !important}'});expect((await noOverflow(page)).scroll,JSON.stringify(await noOverflow(page))).toBeLessThanOrEqual(321);}
 if(viewport.width===390){expect((await new AxeBuilder({page}).withTags(['wcag2a','wcag2aa','wcag21aa']).analyze()).violations).toEqual([]);await page.screenshot({path:info.outputPath('consistent-spacing-390.png'),fullPage:true});}
});

for(const app of ['admin','servant'])test(`${app} personal activity always sends its actor and retains applied filters on pages`,async({page,context},info)=>{
 await authenticate(context,app);await fixtures(context);await page.setViewportSize({width:390,height:900});const requests:URL[]=[];
 page.on('request',r=>{if(r.url().includes('/activity-history/'))requests.push(new URL(r.url()));});
 await page.goto(`/${app}/#/${app==='admin' ? 'dashboard/' : ''}activity-history`);await expect(page.getByRole('heading',{name:'سجل نشاطي ومشاركتي'})).toBeVisible();await expect(page.getByText('جارٍ تحميل السجل…')).not.toBeVisible();
 expect(requests.length).toBeGreaterThan(0);expect(requests.every(r=>r.searchParams.get('actorId')===owner)).toBeTruthy();
 await page.getByLabel('من تاريخ',{exact:true}).fill('2026-10-01');await page.getByLabel('إلى تاريخ (شامل)',{exact:true}).fill('2026-10-30');await page.getByLabel('بحث باسم المخدوم أو الملاحظات',{exact:true}).fill(' مريم ');await page.getByLabel('النتيجة',{exact:true}).selectOption('1');await page.getByLabel('حالة السجل',{exact:true}).selectOption('Active');
 await page.getByRole('button',{name:'تطبيق',exact:true}).click();await expect(page.getByText('جارٍ تحميل السجل…')).not.toBeVisible();let last=requests.at(-1)!;expect(last.searchParams.get('search')).toBe('مريم');expect(last.searchParams.get('dateTo')).toBe('2026-10-30');expect(last.searchParams.get('outcome')).toBe('1');
 await page.getByRole('button',{name:'التالي',exact:true}).click();await expect(page.getByText('جارٍ تحميل السجل…')).not.toBeVisible();last=requests.at(-1)!;expect(last.searchParams.get('page')).toBe('2');expect(last.searchParams.get('dateFrom')).toBe('2026-10-01');
 await page.getByRole('button',{name:'الزيارات والمشاركة',exact:true}).click();await expect(page.getByText('جارٍ تحميل السجل…')).not.toBeVisible();last=requests.at(-1)!;expect(last.pathname.endsWith('/visits')).toBeTruthy();expect(last.searchParams.get('actorId')).toBe(owner);expect(last.searchParams.has('outcome')).toBeFalsy();expect(last.searchParams.get('search')).toBe('مريم');
 const before=requests.length;await page.getByLabel('من تاريخ',{exact:true}).fill('2026-11-01');await page.getByRole('button',{name:'تطبيق',exact:true}).click();await expect(page.getByRole('alert')).toContainText('أدخل فترة صحيحة');expect(requests).toHaveLength(before);
 await page.getByRole('button',{name:'مسح',exact:true}).click();await expect(page.getByRole('alert')).toHaveCount(0);expect(requests.at(-1)!.searchParams.has('dateFrom')).toBeFalsy();expect(requests.at(-1)!.searchParams.get('actorId')).toBe(owner);
 expect((await noOverflow(page)).scroll).toBeLessThanOrEqual(391);expect((await new AxeBuilder({page}).withTags(['wcag2a','wcag2aa','wcag21aa']).analyze()).violations).toEqual([]);await page.evaluate(()=>window.scrollTo(0,0));await page.screenshot({path:info.outputPath(`${app}-personal-activity.png`),fullPage:true});
});
