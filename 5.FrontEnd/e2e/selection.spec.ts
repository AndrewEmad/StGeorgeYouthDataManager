import {test,expect} from '@playwright/test';
import {authenticate,fixtures,id,owner,student} from './fixtures';
const second='33333333-3333-3333-3333-333333333333';
test('attendance page deselection keeps earlier-page choices and saving locks dependent controls',async({page,context},info)=>{
 await authenticate(context,'admin');await fixtures(context);
 await context.route('**/api/meetings/*/roster?*',route=>{const index=Number(new URL(route.request().url()).searchParams.get('page'));return route.fulfill({json:{items:[{id:index===1 ? id : second,personId:index===1 ? id : second,name:index===1 ? 'طالب الصفحة الأولى' : 'طالب الصفحة الثانية',present:false}],totalCount:51}});});
 let submitted:any=null;let release!:()=>void;const gate=new Promise<void>(r=>release=r);
 await context.route('**/api/meetings/*/marks',async route=>{submitted=route.request().postDataJSON();await gate;await route.fulfill({json:{}});});
 await page.goto('/admin/#/dashboard/attendance');await page.getByRole('combobox',{name:'الاجتماع',exact:true}).selectOption(id);
 await page.getByLabel('تحديد هذه الصفحة فقط',{exact:true}).check();
 await page.getByRole('navigation',{name:'صفحات الحضور'}).getByRole('button',{name:'التالي'}).click();
 await expect(page.getByText('طالب الصفحة الثانية',{exact:true})).toBeVisible();
 await page.getByLabel('تحديد هذه الصفحة فقط',{exact:true}).check();await expect(page.getByText('2 اختيار عبر الصفحات')).toBeVisible();
 await page.getByLabel('تحديد هذه الصفحة فقط',{exact:true}).uncheck();await expect(page.getByText('1 اختيار عبر الصفحات')).toBeVisible();
 await page.getByRole('button',{name:'تسجيل حضور',exact:true}).click();await expect.poll(()=>submitted).not.toBeNull();expect(submitted.ids).toEqual([id]);
 await expect(page.getByRole('combobox',{name:'القائمة',exact:true})).toBeDisabled();await expect(page.getByLabel('بحث بالاسم')).toBeDisabled();
 await page.screenshot({path:info.outputPath('attendance-saving.png'),fullPage:true});release();await expect(page.getByText('تم حفظ الحضور وحالة الاجتماع.')).toBeVisible();
});
test('bulk partial assignment retains only failed choices for retry',async({page,context})=>{
 await authenticate(context,'admin');await fixtures(context);
 await context.route('**/api/StudentQueries/paged?*',route=>{const index=Number(new URL(route.request().url()).searchParams.get('page'));return route.fulfill({json:{items:[{...student,id:index===1 ? id : second,fullName:index===1 ? 'طالب أول' : 'طالب ثان'}],totalCount:51,page:index,pageSize:25}});});
 let requests=0;await context.route('**/api/StudentCommands/bulk-assignment',route=>{requests++;const submitted=route.request().postDataJSON();expect(submitted.ids.sort()).toEqual([id,second].sort());return route.fulfill({json:{outcomes:[{studentId:id,success:true},{studentId:second,success:false,error:'راجع الإسناد'}]}});});
 await page.goto('/admin/#/dashboard/students');await page.getByRole('checkbox',{name:'تحديد طالب أول',exact:true}).check();
 await page.getByRole('button',{name:'التالي',exact:true}).last().click();await page.getByRole('checkbox',{name:'تحديد طالب ثان',exact:true}).check();
 await page.getByRole('button',{name:'تخصيص المحددين لخادم'}).click();const dialog=page.getByRole('dialog');await dialog.getByLabel('المسؤول',{exact:true}).selectOption(owner);
 await dialog.getByRole('button',{name:'تأكيد التخصيص',exact:true}).click();await expect(page.getByText('1 مخدوم محدد')).toBeVisible();expect(requests).toBe(1);await expect(dialog).toBeVisible();
});
test('phone actions normalize both fields once and preserve international codes',async({page,context})=>{
 await authenticate(context,'admin');await fixtures(context);await page.goto(`/admin/#/dashboard/students/${id}`);
 await expect(page.locator('a[href="tel:+201012345678"]')).toBeVisible();await expect(page.locator('a[href="tel:+447700900123"]')).toBeVisible();
 await expect(page.locator('a[href="sms:+447700900123"]')).toBeVisible();await expect(page.locator('a[href="https://wa.me/447700900123"]')).toBeVisible();
});
