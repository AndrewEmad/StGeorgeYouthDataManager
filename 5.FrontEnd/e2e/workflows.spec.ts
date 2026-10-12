import {test,expect} from '@playwright/test';
import AxeBuilder from '@axe-core/playwright';
import {authenticate,fixtures,id,session,noOverflow} from './fixtures';

for(const width of [320,360,390,430,768,1024,1440,1920])test(`reachable workflow dialogs fit and expose labelled controls at ${width}px`,async({page,context},info)=>{
 await authenticate(context,'admin');await fixtures(context);await page.setViewportSize({width,height:800});
 for(const flow of [{route:'tasks',button:'مهمة جديدة'},{route:'notifications',button:'جدول جديد'},{route:'servants',button:'إضافة خادم جديد'},{route:'students',button:'إضافة مخدوم'}]){
  await page.goto('/admin/#/dashboard/'+flow.route);await page.getByRole('button',{name:flow.button,exact:true}).click();
  const dialog=page.getByRole('dialog',{includeHidden:false});await expect(dialog).toBeVisible();
  const bounds=await dialog.boundingBox();expect(bounds!.width).toBeLessThanOrEqual(width);expect(bounds!.height).toBeLessThanOrEqual(800);
  expect((await noOverflow(page)).scroll).toBeLessThanOrEqual(width+1);
  const issues=await new AxeBuilder({page}).withTags(['wcag2a','wcag2aa','wcag21aa']).analyze();expect(issues.violations.map(v=>({id:v.id,nodes:v.nodes.map(n=>n.target)}))).toEqual([]);
  await page.screenshot({path:info.outputPath(`${flow.route}-${width}.png`)});await page.keyboard.press('Escape');await expect(dialog).not.toBeVisible();
 }
});

for(const app of ['admin','servant'])test(`${app} onboarding follows server restrictions and preserves failed profile input`,async({page,context})=>{
 await authenticate(context,app,{requiresPasswordChange:true,requiresProfileCompletion:true});await fixtures(context);
 await context.route('**/api/Auth/change-password',route=>route.fulfill({json:session(app,{requiresPasswordChange:false,requiresProfileCompletion:true,refreshToken:'password-replacement'})}));
 await page.goto(`/${app}/#/${app==='admin' ? 'dashboard/tasks' : 'tasks'}`);await expect(page).toHaveURL(/change-password/);
 await page.getByLabel('كلمة المرور الحالية',{exact:true}).fill('password123');await page.getByLabel('كلمة المرور الجديدة',{exact:true}).fill('changedpassword123');await page.getByLabel('تأكيد كلمة المرور الجديدة',{exact:true}).fill('changedpassword123');await page.getByRole('button',{name:'حفظ وتفعيل الحساب'}).click();
 await expect(page).toHaveURL(/complete-profile/);await page.getByLabel('الاسم الكامل',{exact:true}).fill('اسم جديد موثق');await page.getByLabel('رقم التليفون',{exact:true}).fill('01012345678');await page.getByLabel('العنوان',{exact:true}).fill('القاهرة');
 let succeeds=false;await context.route('**/api/Profile',route=>route.request().method()==='PUT' ? route.fulfill(succeeds ? {json:session(app,{fullName:'الاسم المعتمد من الخادم',refreshToken:'profile-replacement'})} : {status:503,json:{message:'تعذر حفظ الملف'}}) : route.fallback());
 await page.getByRole('button',{name:'حفظ والمتابعة'}).click();await expect(page).toHaveURL(/complete-profile/);await expect(page.getByLabel('الاسم الكامل',{exact:true})).toHaveValue('اسم جديد موثق');
 succeeds=true;await page.getByRole('button',{name:'حفظ والمتابعة'}).click();await expect(page).toHaveURL(/dashboard/);
 expect(await page.evaluate(app=>JSON.parse(localStorage.getItem(`ydm.${app}app.user`)!).fullName,app)).toBe('الاسم المعتمد من الخادم');
});

test('existing schedule seconds round trip and save one canonical weekly schedule',async({page,context})=>{
 await authenticate(context,'admin');await fixtures(context);let body:any=null;
 await context.route(`**/api/Notification/schedules/${id}`,route=>{body=route.request().postDataJSON();return route.fulfill({json:{}});});
 await page.goto('/admin/#/dashboard/notifications');await page.getByRole('button',{name:'تعديل',exact:true}).click();
 await expect(page.getByLabel('وقت التذكير')).toHaveValue('18:00');await page.getByRole('dialog').getByRole('button',{name:'حفظ',exact:true}).click();
 await expect.poll(()=>body).toMatchObject({daysOfWeek:'5',timeOfDay:'18:00',isActive:true});await expect(page.getByRole('dialog')).not.toBeVisible();
});

test('a saved user with a failed photo offers upload-only retry without creating another account',async({page,context})=>{
 await authenticate(context,'admin');await fixtures(context);let created=0,uploads=0;
 await context.route('**/api/Users',route=>route.request().method()==='POST' ? (created++,route.fulfill({json:id})) : route.fallback());
 await context.route(`**/api/Users/${id}/photo`,route=>{uploads++;return route.fulfill(uploads===1 ? {status:500,json:{message:'تعذر تخزين الصورة'}} : {json:{photoPath:'verified-avatar.jpg'}});});
 await page.goto('/admin/#/dashboard/servants');await page.getByRole('button',{name:'إضافة خادم جديد',exact:true}).click();
 const dialog=page.getByRole('dialog',{name:'إضافة خادم جديد'});await dialog.getByLabel('الاسم الكامل',{exact:true}).fill('اسم الخادم');await dialog.getByLabel('اسم المستخدم',{exact:true}).fill('newservant');await dialog.getByLabel('البريد الإلكتروني',{exact:true}).fill('new@example.com');await dialog.getByLabel('كلمة المرور',{exact:true}).fill('password123');await dialog.getByLabel('رقم الهاتف',{exact:true}).fill('01012345678');
 const bytes=await page.evaluate(()=>{const canvas=document.createElement('canvas');canvas.width=16;canvas.height=16;canvas.getContext('2d')!.fillRect(0,0,16,16);return canvas.toDataURL('image/png').split(',')[1];});
 await dialog.locator('input[type=file]').setInputFiles({name:'avatar.png',mimeType:'image/png',buffer:Buffer.from(bytes,'base64')});
 await page.getByRole('dialog',{name:'قص الصورة'}).getByRole('button',{name:'تأكيد',exact:true}).click();
 await dialog.getByRole('button',{name:'حفظ البيانات',exact:true}).click();await expect(dialog.getByRole('alert')).toContainText('تم حفظ المستخدم، لكن رفع الصورة فشل');
 await dialog.getByRole('button',{name:'إعادة رفع الصورة فقط'}).click();await expect(dialog).not.toBeVisible();expect(created).toBe(1);expect(uploads).toBe(2);
});

test('failed student save keeps input and exposes a themed error inside the dialog',async({page,context})=>{
 await authenticate(context,'admin');await fixtures(context);await context.route('**/api/StudentCommands',route=>route.fulfill({status:503,json:{message:'تعذر حفظ بيانات الطالب'}}));await page.goto('/admin/#/dashboard/students');await page.getByRole('button',{name:'إضافة مخدوم',exact:true}).click();const dialog=page.getByRole('dialog');await dialog.getByLabel('الاسم الكامل *',{exact:true}).fill('اسم محفوظ');await dialog.getByLabel('رقم التليفون *',{exact:true}).fill('01012345678');await dialog.getByRole('button',{name:'إضافة',exact:true}).click();await expect(dialog.getByRole('alert')).toContainText('تعذر حفظ بيانات الطالب');await expect(dialog.getByLabel('الاسم الكامل *',{exact:true})).toHaveValue('اسم محفوظ');await expect(dialog.getByRole('button',{name:'إضافة',exact:true})).toBeEnabled();
});
test('file read errors leave import retry available without submitting rows',async({page,context})=>{
 await authenticate(context,'admin');await fixtures(context);await page.addInitScript(()=>{File.prototype.arrayBuffer=async()=>{throw new Error('fixture read failure');};});await page.goto('/admin/#/dashboard/students');await page.locator('input[type=file][accept=".csv"]').setInputFiles({name:'unreadable.csv',mimeType:'text/csv',buffer:Buffer.from('FullName,Phone\nاسم,01012345678')});await expect(page.getByText('تعذر قراءة الملف. أعد اختياره.')).toBeVisible();await expect(page.getByRole('button',{name:'استيراد من ملف',exact:true})).toBeEnabled();
});
