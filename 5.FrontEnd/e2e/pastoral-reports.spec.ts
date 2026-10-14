import {test,expect,BrowserContext} from '@playwright/test';
import AxeBuilder from '@axe-core/playwright';
import {authenticate,fixtures,noOverflow,owner,id,longName} from './fixtures';

const definitions={
 'servant-service-summary':{title:'ملخص خدمة الخدام',columns:[['name','الخادم'],['active','الحساب نشط','boolean'],['assignedStudentsCount','مخدوموه الحاليون','number'],['eligible','اجتماعات في قائمة حضوره','number'],['present','حضر','number'],['rate','نسبة حضور الخادم %','percent'],['calls','المكالمات','number'],['callAnswered','مجابة','number'],['callNotAnswered','بلا رد','number'],['callBusy','مشغول','number'],['callSwitchedOff','الهاتف مغلق','number'],['callWrongNumber','رقم خاطئ','number'],['visits','زيارات شارك فيها','number'],['visitSuccessful','زيارات ناجحة','number'],['visitNotAtHome','المخدوم غير موجود','number'],['visitRefusedVisit','زيارات مرفوضة','number'],['visitPostponed','زيارات مؤجلة','number'],['attempted','مخدومون حاول افتقادهم','number'],['reached','مخدومون تواصل معهم بنجاح','number'],['openTasks','متابعات مفتوحة الآن','number'],['overdueTasks','متابعات متأخرة الآن','number']]},
 'group-attendance':{title:'حضور مجموعات الخدام',columns:[['name','خادم المجموعة وقت الاجتماع'],['students','مخدومون مختلفون في القوائم','number'],['eligible','فرص حضور مؤكدة','number'],['present','مرات الحضور','number'],['absent','مرات الغياب','number'],['rate','نسبة حضور المجموعة %','percent']]},
 'pastoral-follow-up':{title:'مخدومون يحتاجون افتقادًا',columns:[['name','المخدوم'],['owner','خادمه الحالي'],['phone','الهاتف','phone'],['reason','سبب المتابعة'],['consecutiveAbsences','غياب متتالي في الفترة','number'],['eligible','فرص حضور مؤكدة في الفترة','number'],['present','مرات الحضور في الفترة','number'],['rate','نسبة الحضور %','percent'],['lastPresent','آخر حضور مؤكد حتى نهاية الفترة','date'],['lastAttempt','آخر محاولة افتقاد حتى نهاية الفترة','datetime'],['lastSuccess','آخر تواصل ناجح حتى نهاية الفترة','datetime'],['openTasks','متابعات مفتوحة الآن','number'],['overdueTasks','متابعات متأخرة الآن','number']]}
};
type Key=keyof typeof definitions;
const keys=Object.keys(definitions) as Key[];
async function reportFixtures(context:BrowserContext,state='populated'){
 await fixtures(context);
 await context.route('**/api/reports/suite/**',route=>{
  const url=new URL(route.request().url()),key=url.pathname.split('/').pop() as Key;
  if(!definitions[key])return route.fallback();
  if(state==='failed')return route.fulfill({status:503,json:{message:'تعذر تحميل تقرير الخدمة'}});
  const definition=definitions[key];
  const rows=state==='empty' ? [] : [{id:key==='pastoral-follow-up' ? id : owner,ownerId:owner,name:longName,owner:longName,phone:'+447700900123',reason:'غياب متتالي، لا يوجد تواصل ناجح حديث، متابعة متأخرة',active:true,assignedStudentsCount:51,students:51,eligible:80,present:52,absent:28,rate:65,calls:12,callAnswered:6,callNotAnswered:2,callBusy:1,callSwitchedOff:2,callWrongNumber:1,visits:3,visitSuccessful:1,visitNotAtHome:1,visitRefusedVisit:0,visitPostponed:1,attempted:9,reached:5,openTasks:4,overdueTasks:2,consecutiveAbsences:3,lastPresent:'2026-10-02',lastAttempt:'2026-10-09T12:00:00Z',lastSuccess:null}];
  return route.fulfill({json:{title:definition.title,period:'2026-10-01 – 2026-10-12 (القاهرة)',scope:'الصفحة الحالية؛ الإجماليات لكل النتائج',columns:definition.columns.map(([key,label,type])=>({key,label,type:type || 'text'})),rows,totalCount:rows.length,page:1,pageSize:25,totals:{eligible:rows.length ? 80 : 0,present:rows.length ? 52 : 0,rate:rows.length ? 65 : null}}});
 });
}
for(const viewport of [...[320,360,390,430,768,1024,1440,1920].map(width=>({width,height:900})),{width:768,height:360},{width:1024,height:360}])test(`church reports and catalog fit ${viewport.width}x${viewport.height}`,async({page,context},info)=>{
 await authenticate(context,'admin');await reportFixtures(context);await page.setViewportSize(viewport);await page.clock.setFixedTime(new Date('2026-10-12T08:00:00Z'));
 await page.goto('/admin/#/dashboard/reports');await expect(page.locator('main h1')).toHaveText('تقارير خدمة الشباب');
 await expect(page.locator('main .ydm-report-group')).toHaveCount(4);await expect(page.locator('main .ydm-report-card')).toHaveCount(17);
 expect((await noOverflow(page)).scroll).toBeLessThanOrEqual(viewport.width+1);
 for(const key of keys){
  await page.goto('/admin/#/dashboard/report-suite?report='+key);await expect(page.locator('main h1')).toHaveText(definitions[key].title);await expect(page.getByText('جارٍ تحميل النتائج…')).not.toBeVisible();
  await expect(page.getByRole('combobox',{name:'عام الخدمة',exact:true})).toHaveValue('');await expect(page.getByLabel('من تاريخ',{exact:true})).toHaveValue('');await expect(page.getByLabel('إلى تاريخ (شامل)',{exact:true})).toHaveValue('');
  expect((await noOverflow(page)).scroll).toBeLessThanOrEqual(viewport.width+1);
  const closeNav=page.getByRole('button',{name:'القائمة',exact:true});if(viewport.width<=768){await closeNav.click();const drawer=page.getByRole('dialog');await expect(drawer).toBeVisible();await page.keyboard.press('Escape');await expect(drawer).not.toBeVisible();}
  if(viewport.width===390 || viewport.width===1440)await expect(page).toHaveScreenshot(`${key}-${viewport.width}.png`,{fullPage:true,animations:'disabled',maxDiffPixelRatio:.015});
 }
 if(viewport.width===320){await page.addStyleTag({content:'html{font-size:200% !important}'});expect((await noOverflow(page)).scroll).toBeLessThanOrEqual(321);await page.getByRole('button',{name:'القائمة',exact:true}).click();const link=page.getByRole('dialog').getByRole('link',{name:'تصدير البيانات',exact:true});await link.scrollIntoViewIfNeeded();await expect(link).toBeInViewport();await page.keyboard.press('Escape');}
 if(viewport.width===390){const result=await new AxeBuilder({page}).withTags(['wcag2a','wcag2aa','wcag21aa']).analyze();expect(result.violations).toEqual([]);}
});

test('one catalog preserves old routes and links summary to the correct person group attendance activity and tasks',async({page,context})=>{
 await authenticate(context,'admin');await reportFixtures(context);await page.setViewportSize({width:1440,height:900});
 await page.goto('/admin/#/dashboard/reports/servant-follow-up');await expect(page).toHaveURL(/#\/dashboard\/reports$/);await expect(page.locator('main h1')).toHaveText('تقارير خدمة الشباب');
 await page.locator('.ydm-report-card').filter({has:page.getByRole('heading',{name:'ملخص خدمة الخدام',exact:true})}).click();await expect(page.getByLabel('التقرير',{exact:true})).toHaveValue('servant-service-summary');
 const name=page.locator('.ydm-table tbody tr').first().getByRole('link',{name:longName,exact:true});await name.click();await expect(page).toHaveURL(new RegExp('servants/'+owner));
 for(const [column,target] of [[2,'students'],[3,'report-suite'],[6,'reports/servant-activity'],[19,'report-suite']] as const){
  await page.goto('/admin/#/dashboard/report-suite?report=servant-service-summary&dateFrom=2026-10-01&dateTo=2026-10-12');await page.locator('.ydm-table tbody tr').first().locator('td').nth(column).getByRole('link').first().click();await expect(page).toHaveURL(new RegExp(target+'.*servantId='+owner));
 }
 await page.goto('/admin/#/dashboard/report-suite?report=servant-service-summary&dateFrom=2026-10-01&dateTo=2026-10-12');await page.locator('.ydm-table tbody tr').first().getByRole('link',{name:'حضور المجموعة',exact:true}).click();await expect(page).toHaveURL(new RegExp('report=group-attendance.*servantId='+owner));await expect(page.getByLabel('من تاريخ',{exact:true})).toHaveValue('2026-10-01');
 await page.goto('/admin/#/dashboard/report-suite?report=group-attendance&dateFrom=2026-10-01&dateTo=2026-10-12&area=القاهرة');await page.locator('.ydm-table tbody tr').first().getByRole('link',{name:longName,exact:true}).click();await expect(page).toHaveURL(/report=attendance/);await expect(page.getByLabel('من تاريخ',{exact:true})).toHaveValue('2026-10-01');
 await page.goto('/admin/#/dashboard/report-suite?report=pastoral-follow-up');await page.locator('.ydm-table tbody tr').first().getByRole('link',{name:longName,exact:true}).first().click();await expect(page).toHaveURL(new RegExp('students/'+id));await page.getByRole('link',{name:'متابعة افتقاد جديدة',exact:true}).click();await expect(page.getByRole('dialog',{name:'مهمة جديدة'})).toBeVisible();
});

test('new report exports and print retain applied filters and invalid absence limits do not submit',async({page,context})=>{
 await authenticate(context,'admin');await reportFixtures(context);await page.addInitScript(()=>{window.print=()=>{};});
 const urls:string[]=[];page.on('request',r=>{if(r.url().includes('/api/reports/suite/'))urls.push(r.url());});
 await context.route('**/api/reports/suite/*/export?**',route=>route.fulfill({body:'export',contentType:'text/csv',headers:{'content-disposition':'attachment; filename=report.csv'}}));
 for(const key of keys){
  await page.goto(`/admin/#/dashboard/report-suite?report=${key}&servantId=${owner}&dateFrom=2026-10-01&dateTo=2026-10-12`);await expect(page.getByText('جارٍ تحميل النتائج…')).not.toBeVisible();
  await page.getByLabel('من تاريخ',{exact:true}).fill('2025-01-01');
  for(const label of ['Excel · كل النتائج','CSV · كل النتائج']){const download=page.waitForEvent('download');await page.getByRole('button',{name:label,exact:true}).click();await download;}
  await page.getByRole('button',{name:'طباعة كل النتائج',exact:true}).click();await expect.poll(()=>urls.some(u=>u.includes(key) && u.includes('full=true'))).toBeTruthy();await page.evaluate(()=>dispatchEvent(new Event('afterprint')));
  const exported=urls.filter(u=>u.includes(key+'/export') || u.includes(key+'?') && u.includes('full=true'));expect(exported).toHaveLength(3);for(const u of exported){const p=new URL(u).searchParams;expect(p.get('dateFrom')).toBe('2026-10-01');expect(p.get('dateTo')).toBe('2026-10-12');expect(p.get('servantId')).toBe(owner);}
 }
 await page.goto('/admin/#/dashboard/report-suite?report=pastoral-follow-up');await expect(page.getByText('جارٍ تحميل النتائج…')).not.toBeVisible();const count=urls.length;await page.getByLabel('حد الغياب المتتالي (اجتماعات)',{exact:true}).fill('0');await page.getByRole('button',{name:'تطبيق',exact:true}).click();expect(urls.length).toBe(count);
});

test('new reports have honest empty and failed states and are unavailable in the servant app',async({page,context})=>{
 await authenticate(context,'admin');await reportFixtures(context,'empty');await page.goto('/admin/#/dashboard/report-suite?report=pastoral-follow-up');await expect(page.getByText('لا توجد نتائج مطابقة لهذه الفلاتر.')).toBeVisible();
 await reportFixtures(context,'failed');await page.reload();await expect(page.getByRole('alert')).toContainText('تعذر تحميل تقرير الخدمة');await expect(page.locator('ydm-report-table')).toHaveCount(0);
 await authenticate(context,'servant');const newRequests:string[]=[];page.on('request',r=>{if(keys.some(key=>r.url().includes('/reports/suite/'+key)))newRequests.push(r.url());});
 await page.goto('/servant/#/report-suite?report=servant-service-summary');await expect(page.getByRole('alert')).toContainText('هذا التقرير غير متاح لهذا الحساب.');await expect(page.getByLabel('التقرير',{exact:true}).locator('option[value="servant-service-summary"]')).toHaveCount(0);expect(newRequests).toHaveLength(0);
});

test('applying report filters fetches once and survives refresh and returning from details',async({page,context})=>{
 await authenticate(context,'admin');await reportFixtures(context);const requests:string[]=[];page.on('request',r=>{if(r.url().includes('/reports/suite/pastoral-follow-up?'))requests.push(r.url());});
 await page.goto('/admin/#/dashboard/report-suite?report=pastoral-follow-up');await expect(page.getByText('جارٍ تحميل النتائج…')).not.toBeVisible();const before=requests.length;
 await page.getByLabel('من تاريخ',{exact:true}).fill('2026-09-01');await page.getByLabel('حد الغياب المتتالي (اجتماعات)',{exact:true}).fill('4');await page.getByRole('button',{name:'تطبيق',exact:true}).click();await expect(page).toHaveURL(/dateFrom=2026-09-01/);await expect(page.getByText('جارٍ تحميل النتائج…')).not.toBeVisible();expect(requests.length-before).toBe(1);
 await page.reload();await expect(page.getByLabel('من تاريخ',{exact:true})).toHaveValue('2026-09-01');await expect(page.getByLabel('حد الغياب المتتالي (اجتماعات)',{exact:true})).toHaveValue('4');
 await page.setViewportSize({width:1440,height:900});await page.locator('.ydm-table tbody tr').first().getByRole('link',{name:longName,exact:true}).first().click();await expect(page).toHaveURL(new RegExp('students/'+id));await page.goBack();await expect(page.getByLabel('من تاريخ',{exact:true})).toHaveValue('2026-09-01');await expect(page.getByLabel('حد الغياب المتتالي (اجتماعات)',{exact:true})).toHaveValue('4');
});
