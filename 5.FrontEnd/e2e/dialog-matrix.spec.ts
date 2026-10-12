import {test,expect} from '@playwright/test';
import AxeBuilder from '@axe-core/playwright';
import {authenticate,fixtures,id,owner,student,noOverflow} from './fixtures';
const cases=[
 {route:'tasks',buttons:['إنجاز وتسجيل النشاط','تغيير الموعد','إعادة إسناد','إلغاء']},
 {route:'notifications',buttons:['تعديل']},
 {route:'request-history',buttons:['مراجعة التفاصيل','موافقة','رفض','سحب الطلب']},
 {route:`students/${id}`,buttons:['تعديل البيانات','تسجيل مكالمة','تسجيل زيارة','تصحيح','إلغاء السجل','عرض صورة الطالب']},
 {route:'attendance',buttons:['اعتماد الحضور','إلغاء الاجتماع','إعداد يوم الاجتماع وأسبوع التقارير','مراجعة كشف المؤهلين التاريخي'],prepare:'meeting'},
 {route:'servants',buttons:['تعديل','حذف']},
 {route:`servants/${owner}`,buttons:['تغيير كلمة المرور']},
 {route:'students',buttons:['تعديل','تخصيص','أرشفة','استيراد من ملف']},
 {route:'archived-students',buttons:['استعادة']},
 {app:'servant',route:'dashboard',buttons:['إرسال واتساب جماعي','إرسال SMS جماعي']},
 {app:'servant',route:`students/${id}`,buttons:['طلب إزالة / إلغاء إسناد']},
];
for(const width of [320,360,390,430,768,1024,1440,1920])test(`remaining reachable dialogs are usable and accessible at ${width}px`,async({page,context},info)=>{
 test.setTimeout(180000);await authenticate(context,'admin');await authenticate(context,'servant');await fixtures(context);await page.setViewportSize({width,height:844});
 await context.route('**/api/StudentQueries/*/photo',route=>route.fulfill({contentType:'image/png',body:Buffer.from('iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+a8n0AAAAASUVORK5CYII=','base64')}));
 await context.route(`**/api/StudentQueries/${id}`,route=>route.fulfill({json:{...student,photoPath:'test.png'}}));
 await context.route('**/api/meetings?*',route=>route.fulfill({json:{items:[{id,date:'2026-10-09',status:'Draft',rosterKnown:false,version:'AAAAAAAABAE='}],totalCount:1}}));
 let number=0;
 for(const flow of cases){const app=flow.app || 'admin';for(const button of flow.buttons){
  await page.goto(`/${app}/#/${app==='admin' ? 'dashboard/' : ''}${flow.route}`);
  if(flow.prepare==='meeting')await page.getByRole('combobox',{name:'الاجتماع',exact:true}).selectOption(id);
  const trigger=page.getByRole('button',{name:button,exact:true}).first();await trigger.click();const dialog=page.getByRole('dialog').last();await expect(dialog).toBeVisible();
  const bounds=await dialog.boundingBox();expect(bounds!.width).toBeLessThanOrEqual(width);expect(bounds!.height).toBeLessThanOrEqual(844);expect((await noOverflow(page)).scroll).toBeLessThanOrEqual(width+1);
  const result=await new AxeBuilder({page}).withTags(['wcag2a','wcag2aa','wcag21aa']).analyze();expect(result.violations.map(v=>({button,id:v.id,nodes:v.nodes.map(n=>n.target)}))).toEqual([]);
  await page.screenshot({path:info.outputPath(`${++number}-${flow.route.replaceAll('/','-')}.png`)});await page.keyboard.press('Escape');await expect(dialog).not.toBeVisible();
 }}
 // Exercise conditional dialogs that do not appear in the default open queues.
 await context.route('**/api/follow-up-tasks?*',route=>route.fulfill({json:{items:[{id,studentId:id,studentName:student.fullName,assigneeId:owner,type:'Call',status:'PendingReview',dueAt:'2026-10-10T08:00:00Z',version:student.version}],totalCount:1}}));
 await page.goto('/admin/#/dashboard/tasks');await page.getByRole('button',{name:'مراجعة وتفعيل',exact:true}).click();let dialog=page.getByRole('dialog');await expect(dialog).toBeVisible();expect((await noOverflow(page)).scroll).toBeLessThanOrEqual(width+1);expect((await new AxeBuilder({page}).withTags(['wcag2a','wcag2aa','wcag21aa']).analyze()).violations).toEqual([]);await page.keyboard.press('Escape');
 await page.goto('/admin/#/dashboard/students');await page.locator('input[type=file][accept=".csv"]').setInputFiles({name:'quoted.csv',mimeType:'text/csv',buffer:Buffer.from('FullName,Phone,Notes\n"اسم، طويل",01012345678,"سطر أول\nسطر ثان"\n,01012345678,invalid')});dialog=page.getByRole('dialog',{name:'معاينة ملف الاستيراد'});await expect(dialog).toBeVisible();await expect(dialog).toContainText('صفوف صالحة: 1 من 2');expect((await noOverflow(page)).scroll).toBeLessThanOrEqual(width+1);expect((await new AxeBuilder({page}).withTags(['wcag2a','wcag2aa','wcag21aa']).analyze()).violations).toEqual([]);await page.keyboard.press('Escape');await expect(dialog).not.toBeVisible();
 for(const app of ['admin','servant']){await page.goto(`/${app}/#/${app==='admin' ? 'dashboard/stats' : 'dashboard'}`);await expect(page.locator('main').getByText('51',{exact:true}).first()).toBeVisible();await page.waitForLoadState('networkidle');await page.getByRole('button',{name:'تثبيت التطبيق',exact:true}).click();dialog=page.getByRole('dialog');await expect(dialog).toBeVisible();expect((await noOverflow(page)).scroll).toBeLessThanOrEqual(width+1);expect((await new AxeBuilder({page}).withTags(['wcag2a','wcag2aa','wcag21aa']).analyze()).violations).toEqual([]);await page.keyboard.press('Escape');}
 if(width<1024){await page.goto('/admin/#/dashboard/stats');await page.getByRole('button',{name:'القائمة',exact:true}).click();await expect(page.getByRole('dialog',{name:'قائمة الإدارة'})).toBeVisible();await page.keyboard.press('Escape');}
});
