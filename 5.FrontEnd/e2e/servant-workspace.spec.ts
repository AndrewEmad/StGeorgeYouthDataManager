import {test,expect} from '@playwright/test';
import AxeBuilder from '@axe-core/playwright';
import {authenticate,fixtures,noOverflow,owner} from './fixtures';

const reports=[
 ['attendance','حضور مجموعتي والغياب'],['attendance-history','سجل حضور مجموعتي'],
 ['contact','افتقادي لمجموعتي'],['follow-up','متابعاتي'],
 ['servant-attendance','حضوري'],['servant-attendance-history','سجل حضوري'],
 ['data-quality','بيانات مجموعتي الناقصة']
];
const management=['workload','workload-owners','approvals','meeting-trends','servant-service-summary','group-attendance','pastoral-follow-up'];

for(const role of ['Servant','Manager'])test(`${role} sees only personal reports and cannot open management report links`,async({page,context})=>{
 await authenticate(context,'servant',{role});await fixtures(context);
 const requests:string[]=[];page.on('request',r=>{if(r.url().includes('/reports/suite/'))requests.push(r.url());});
 await page.goto('/servant/#/report-suite');await expect(page.getByText('جارٍ تحميل النتائج…')).not.toBeVisible();
 const options=page.getByLabel('التقرير',{exact:true}).locator('option');
 expect(await options.allTextContents()).toEqual([reports[0][1],reports[1][1],reports[2][1],reports[6][1],reports[3][1],reports[4][1],reports[5][1]]);
 await expect(page.locator('ydm-resource-picker[kind="report-actors"]')).toHaveCount(0);
 for(const key of management){await page.goto('/servant/#/report-suite?report='+key);await expect(page.getByRole('alert')).toContainText('هذا التقرير غير متاح لهذا الحساب.');}
 expect(requests.filter(u=>management.some(key=>new URL(u).pathname.endsWith('/'+key)))).toEqual([]);
 for(const key of ['servants','operations','backups','reports/servant-activity']){await page.goto('/servant/#/'+key);await expect(page).toHaveURL(/#\/dashboard$/);}
});

test('personal workspace keeps tasks requests activity and group scoped even for a manager account',async({page,context})=>{
 await authenticate(context,'servant',{role:'Manager'});await fixtures(context);
 const requests:{url:URL;headers:Record<string,string>}[]=[];
 page.on('request',r=>{if(r.url().includes('/api/'))requests.push({url:new URL(r.url()),headers:r.headers()});});
 await page.goto('/servant/#/dashboard');await expect(page.getByRole('link',{name:'تقارير مجموعتي وحضوري',exact:true})).toBeVisible();
 await page.locator('.stats-grid a').last().click();await expect(page).toHaveURL(/report=contact.*issue=never-attempted/);
 await page.goto('/servant/#/students');await expect(page.getByLabel('قائمة المخدومين')).toHaveValue('Assigned');
 await expect.poll(()=>requests.some(r=>r.url.pathname.endsWith('/students-for-servant') && r.url.searchParams.get('segment')==='Assigned')).toBeTruthy();
 await page.getByLabel('قائمة المخدومين').selectOption('Unassigned');
 await expect.poll(()=>requests.some(r=>r.url.searchParams.get('segment')==='Unassigned')).toBeTruthy();
 await page.getByRole('button',{name:'مسح',exact:true}).click();await expect(page.getByLabel('قائمة المخدومين')).toHaveValue('Assigned');
 await page.goto('/servant/#/tasks');await expect(page.locator('main h1')).toHaveText('متابعاتي');
 await expect(page.getByText('قائمة إعادة الإسناد',{exact:true})).toHaveCount(0);await expect(page.getByRole('button',{name:'إعادة الإسناد',exact:true})).toHaveCount(0);
 await page.getByRole('button',{name:'مهمة جديدة',exact:true}).click();const dialog=page.getByRole('dialog');await expect(dialog).toBeVisible();
 await expect(dialog.locator('ydm-resource-picker[kind="servants"]')).toHaveCount(0);await page.keyboard.press('Escape');
 await page.goto('/servant/#/requests?mine=false');await expect(page.locator('main h1')).toHaveText('طلباتي');
 await expect(page.getByRole('checkbox',{name:'طلباتي فقط'})).toHaveCount(0);await expect(page.getByRole('button',{name:'موافقة',exact:true})).toHaveCount(0);
 await expect.poll(()=>requests.some(r=>r.url.pathname.endsWith('/approvals/all') && r.url.searchParams.get('mine')==='true')).toBeTruthy();
 await page.goto('/servant/#/activity-history');await expect(page.locator('main h1')).toHaveText('سجل نشاطي ومشاركتي');
 await expect.poll(()=>requests.some(r=>r.url.pathname.endsWith('/activity-history/calls') && r.url.searchParams.get('actorId')===owner)).toBeTruthy();
 expect(requests.every(r=>r.headers['x-ydm-workspace']==='personal')).toBeTruthy();
});

test('personal report exports and print use applied filters and ignore another actor in a copied URL',async({page,context})=>{
 await authenticate(context,'servant',{role:'Manager'});await fixtures(context);await page.addInitScript(()=>{window.print=()=>{};});
 const requests:{url:URL;headers:Record<string,string>}[]=[];
 page.on('request',r=>{if(r.url().includes('/reports/suite/'))requests.push({url:new URL(r.url()),headers:r.headers()});});
 await context.route('**/api/reports/suite/*/export?**',r=>r.fulfill({body:'export',contentType:'text/csv',headers:{'content-disposition':'attachment; filename=personal.csv'}}));
 await page.goto('/servant/#/report-suite?report=contact&servantId=33333333-3333-3333-3333-333333333333&dateFrom=2026-10-01&dateTo=2026-10-12');
 await expect(page.getByText('جارٍ تحميل النتائج…')).not.toBeVisible();await page.getByLabel('من تاريخ',{exact:true}).fill('2025-01-01');
 for(const name of ['Excel · كل النتائج','CSV · كل النتائج']){const download=page.waitForEvent('download');await page.getByRole('button',{name,exact:true}).click();await download;}
 await page.getByRole('button',{name:'طباعة كل النتائج',exact:true}).click();await expect.poll(()=>requests.some(r=>r.url.searchParams.get('full')==='true')).toBeTruthy();
 for(const r of requests){expect(r.headers['x-ydm-workspace']).toBe('personal');expect(r.url.searchParams.has('servantId')).toBeFalsy();expect(r.url.searchParams.get('dateFrom')).toBe('2026-10-01');expect(r.url.searchParams.get('dateTo')).toBe('2026-10-12');}
 await page.goto('/servant/#/report-suite?report=servant-attendance&search=another-user');await expect(page.locator('main h1')).toHaveText('حضوري');await expect(page.getByRole('textbox',{name:'بحث باسم الخادم'})).toHaveCount(0);await expect.poll(()=>requests.filter(r=>r.url.pathname.endsWith('/servant-attendance')).length).toBe(1);expect(requests.at(-1)!.url.searchParams.has('search')).toBeFalsy();
});

for(const viewport of [...[320,360,390,430,768,1024,1440,1920].map(width=>({width,height:900})),{width:768,height:360},{width:1024,height:360}])test(`personal workspace fits ${viewport.width}x${viewport.height}`,async({page,context},info)=>{
 await authenticate(context,'servant',{role:'Servant'});await fixtures(context);await page.setViewportSize(viewport);
 for(const path of ['dashboard','students','tasks','requests','activity-history','notifications','profile','request-add-student']){
  await page.goto('/servant/#/'+path);await expect(page.locator('main')).toBeVisible();
  await expect(page.locator('main').getByText(/جارٍ تحميل/).first()).not.toBeVisible();
  expect((await noOverflow(page)).scroll).toBeLessThanOrEqual(viewport.width+1);
 }
 for(const [key,name] of reports){await page.goto('/servant/#/report-suite?report='+key);await expect(page.locator('main h1')).toHaveText(name);await expect(page.getByText('جارٍ تحميل النتائج…')).not.toBeVisible();expect((await noOverflow(page)).scroll).toBeLessThanOrEqual(viewport.width+1);}
 if(viewport.width===320){await page.addStyleTag({content:'html{font-size:200% !important}'});expect((await noOverflow(page)).scroll).toBeLessThanOrEqual(321);}
 if(viewport.width===390){
  const result=await new AxeBuilder({page}).withTags(['wcag2a','wcag2aa','wcag21aa']).analyze();expect(result.violations).toEqual([]);
  await page.screenshot({path:info.outputPath('personal-report-390.png'),fullPage:true});
  await page.goto('/servant/#/tasks');await page.getByRole('button',{name:'مهمة جديدة',exact:true}).click();await expect(page.getByRole('dialog')).toBeVisible();expect((await noOverflow(page)).scroll).toBeLessThanOrEqual(391);await page.keyboard.press('Escape');await expect(page.getByRole('button',{name:'مهمة جديدة',exact:true})).toBeFocused();
 }
});
