import {test,expect,type Locator} from '@playwright/test';
import {authenticate,fixtures,id,student} from './fixtures';
import {studentAreas} from '../shared/core/student-areas';

async function choices(select:Locator){
 await expect(select).toHaveJSProperty('tagName','SELECT');
 const labels=await select.locator('option').allTextContents();
 expect(labels.filter(label=>studentAreas.includes(label as any))).toEqual([...studentAreas]);
 expect(labels.filter(label=>!['غير مسجلة','اترك القيمة الحالية','اختر المنطقة'].includes(label))).toEqual([...studentAreas]);
}
test('Admin add edit and bulk controls share the exact area dropdown',async({page,context})=>{
 await authenticate(context,'admin');await fixtures(context);await context.route('**/api/StudentQueries/'+id+'?*',route=>route.fulfill({json:{...student,area:'المرج الجديده'}}));await page.goto('/admin/#/dashboard/students');
 await page.getByRole('button',{name:'إضافة مخدوم',exact:true}).click();let dialog=page.getByRole('dialog');let select=dialog.getByLabel('المنطقة',{exact:true});await choices(select);await select.selectOption('اخرى');await expect(select).toHaveValue('اخرى');await page.keyboard.press('Escape');
 await page.getByRole('button',{name:'تعديل',exact:true}).first().click();dialog=page.getByRole('dialog');select=dialog.getByLabel('المنطقة',{exact:true});await choices(select);await expect(select).toHaveValue('اخرى');await page.keyboard.press('Escape');
 await page.getByRole('checkbox',{name:'تحديد '+student.fullName,exact:true}).check();await page.getByRole('button',{name:'تغيير المنطقة للمحددين',exact:true}).click();dialog=page.getByRole('dialog');await choices(dialog.getByLabel('المنطقة الجديدة',{exact:true}));
});
for(const app of ['admin','servant'] as const)test(`${app} detail normalizes a known legacy area and maps an unknown area to other when saving`,async({page,context})=>{
 await authenticate(context,app);await fixtures(context);let raw='شبرا الخيمه';await context.route('**/api/StudentQueries/'+id+'?*',route=>route.fulfill({json:{...student,area:raw}}));
 const root=app==='admin' ? '/dashboard' : '';await page.goto(`/${app}/#${root}/students/${id}`);await page.getByRole('button',{name:'تعديل البيانات',exact:true}).click();let dialog=page.getByRole('dialog');let select=dialog.getByRole('combobox',{name:/^المنطقة/});await choices(select);await expect(select).toHaveValue('شبرا البلد');await page.keyboard.press('Escape');
 raw='منطقة قديمة';await page.reload();await page.getByRole('button',{name:'تعديل البيانات',exact:true}).click();dialog=page.getByRole('dialog');select=dialog.getByRole('combobox',{name:/^المنطقة/});await choices(select);await expect(select).toHaveValue('اخرى');await expect(dialog).not.toContainText('القيمة الحالية تحتاج مراجعة: منطقة قديمة');
 let body:any;await context.route('**/api/StudentCommands/'+id,route=>{body=route.request().postDataJSON();return route.fulfill({json:{}});});await dialog.getByRole('button',{name:'حفظ',exact:true}).click();await expect.poll(()=>body).toBeDefined();expect(body.area).toBe('اخرى');
 await expect(dialog).not.toBeVisible();await page.getByRole('button',{name:'تعديل البيانات',exact:true}).click();dialog=page.getByRole('dialog');await dialog.getByRole('combobox',{name:/^المنطقة/}).selectOption({label:'اخرى'});body=undefined;await dialog.getByRole('button',{name:'حفظ',exact:true}).click();await expect.poll(()=>body?.area).toBe('اخرى');
});
test('servant addition request uses the fixed choices without accepting arbitrary typed areas',async({page,context})=>{
 await authenticate(context,'servant');await fixtures(context);await page.goto('/servant/#/request-add-student');await choices(page.getByLabel('المنطقة',{exact:true}));
});

for(const target of [
 {app:'admin',route:'/admin/#/dashboard/students',list:true},
 {app:'servant',route:'/servant/#/students',list:true},
 {app:'admin',route:'/admin/#/dashboard/report-suite?report=attendance',list:false},
 {app:'servant',route:'/servant/#/report-suite?report=contact',list:false},
] as const)test(`${target.app} ${target.list ? 'student' : 'report'} filters use the approved regions rather than database values`,async({page,context})=>{
 await authenticate(context,target.app);await fixtures(context);await page.goto(target.route);
 if(target.app==='admin' && target.list)await page.locator('app-filters-bar').getByRole('button').first().click();
 const select=target.app==='admin' && target.list ? page.locator('app-filters-bar select').first() : page.getByRole('combobox',{name:'المنطقة',exact:true});
 await expect(select).toBeVisible();
 expect((await select.locator('option').allTextContents()).filter(label=>!['— الكل —','الكل','كل المناطق','منطقة غير مسجلة'].includes(label))).toEqual([...studentAreas]);
 await select.selectOption('اخرى');await expect(select).toHaveValue('اخرى');
});
