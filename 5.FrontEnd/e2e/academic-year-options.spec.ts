import {test,expect,type Locator} from '@playwright/test';
import {authenticate,fixtures,id,student} from './fixtures';
import {academicYears} from '../shared/core/academic-years';

async function choices(select:Locator){
 await expect(select).toHaveJSProperty('tagName','SELECT');
 const labels=await select.locator('option').allTextContents();
 expect(labels.filter(label=>academicYears.includes(label as any))).toEqual([...academicYears]);
 expect(labels.filter(label=>!['غير مسجلة','اترك القيمة الحالية','اختر السنة الدراسية'].includes(label))).toEqual([...academicYears]);
}
test('Admin add edit and bulk controls share the exact academic year dropdown',async({page,context})=>{
 await authenticate(context,'admin');await fixtures(context);await page.goto('/admin/#/dashboard/students');
 await page.getByRole('button',{name:'إضافة مخدوم',exact:true}).click();let dialog=page.getByRole('dialog');let select=dialog.getByLabel('السنة الدراسية',{exact:true});await choices(select);await select.selectOption('غير معروف');await expect(select).toHaveValue('غير معروف');await page.keyboard.press('Escape');
 await page.getByRole('button',{name:'تعديل',exact:true}).first().click();dialog=page.getByRole('dialog');select=dialog.getByLabel('السنة الدراسية',{exact:true});await choices(select);await expect(select).toHaveValue('سنة ثالثة');await page.keyboard.press('Escape');
 await page.getByRole('checkbox',{name:'تحديد '+student.fullName,exact:true}).check();await page.getByRole('button',{name:'تغيير السنة الدراسية للمحددين',exact:true}).click();dialog=page.getByRole('dialog');await choices(dialog.getByLabel('السنة الدراسية الجديدة',{exact:true}));
});
for(const app of ['admin','servant'] as const)test(`${app} detail normalizes a known legacy year and preserves an unknown year until explicitly changed`,async({page,context})=>{
 await authenticate(context,app);await fixtures(context);let raw='سنة أولي';await context.route('**/api/StudentQueries/'+id+'?*',route=>route.fulfill({json:{...student,academicYear:raw}}));
 const root=app==='admin' ? '/dashboard' : '';await page.goto(`/${app}/#${root}/students/${id}`);await page.getByRole('button',{name:'تعديل البيانات',exact:true}).click();let dialog=page.getByRole('dialog');let select=dialog.getByRole('combobox',{name:/^السنة الدراسية/});await choices(select);await expect(select).toHaveValue('سنة اولى');await page.keyboard.press('Escape');
 raw='أولى ثانوي';await page.reload();await page.getByRole('button',{name:'تعديل البيانات',exact:true}).click();dialog=page.getByRole('dialog');select=dialog.getByRole('combobox',{name:/^السنة الدراسية/});await choices(select);await expect(dialog).toContainText('القيمة الحالية تحتاج مراجعة: أولى ثانوي');
 let body:any;await context.route('**/api/StudentCommands/'+id,route=>{body=route.request().postDataJSON();return route.fulfill({json:{}});});await dialog.getByRole('button',{name:'حفظ',exact:true}).click();await expect.poll(()=>body).toBeDefined();expect(body).not.toHaveProperty('academicYear');
 await expect(dialog).not.toBeVisible();await page.getByRole('button',{name:'تعديل البيانات',exact:true}).click();dialog=page.getByRole('dialog');await dialog.getByRole('combobox',{name:/^السنة الدراسية/}).selectOption({label:'غير معروف'});body=undefined;await dialog.getByRole('button',{name:'حفظ',exact:true}).click();await expect.poll(()=>body?.academicYear).toBe('غير معروف');
});
test('servant addition request uses the fixed choices without accepting arbitrary typed years',async({page,context})=>{
 await authenticate(context,'servant');await fixtures(context);await page.goto('/servant/#/request-add-student');await choices(page.getByLabel('السنة الدراسية',{exact:true}));
});
