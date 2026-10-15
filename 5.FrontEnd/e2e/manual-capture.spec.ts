import {test,expect,Page,Locator} from '@playwright/test';
import {mkdirSync,writeFileSync,readFileSync} from 'node:fs';
import {resolve} from 'node:path';
import {authenticate} from './fixtures';
import {manualFixtures,id,owner} from './manual-demo';
import {topics as adminTopics} from '../shared/help/admin';
import {topics as servantTopics} from '../shared/help/servant';

test.skip(process.env['YDM_CAPTURE_MANUAL']!=='1','Run explicitly to regenerate fictional user-manual screenshots.');
for(const app of ['admin','servant'])test(`capture complete ${app} user manual`,async({page,context})=>{
 test.setTimeout(240000);
 const role=app==='admin' ? 'Manager' : 'Secretary';
 const token='test.'+Buffer.from(JSON.stringify({exp:1893456000,sub:owner,role})).toString('base64url')+'.signature';
 await authenticate(context,app,{token,fullName:'يوسف جرجس',role});await manualFixtures(context,app);
 await page.clock.setFixedTime(new Date('2026-10-15T08:00:00Z'));await page.setViewportSize({width:1440,height:1000});
 const root=app==='admin' ? 'dashboard/' : '',dir=resolve('shared/help/assets',app);mkdirSync(dir,{recursive:true});
 const captured:Record<string,{route:string;kind:string;imageWidth:number;imageHeight:number}>={};
 const go=async(path:string)=>{await page.goto(`/${app}/#/${root}${path}`,{waitUntil:'domcontentloaded'});await expect(page.locator('main')).toBeVisible();await expect(page.locator('app-loader')).toHaveCount(0);await expect(page.locator('main').getByText(/^جارٍ تحميل/).first()).not.toBeVisible();await page.evaluate(()=>document.fonts.ready);};
 const shot=async(file:string,region?:Locator)=>{
  await page.evaluate(()=>document.fonts.ready);await page.evaluate(()=>{let e=document.getElementById('manual-demo');if(!e){e=document.createElement('div');e.id='manual-demo';e.textContent='مثال توضيحي · بيانات تجريبية';e.style.cssText='position:fixed;bottom:8px;left:8px;z-index:2147483647;background:#312e81;color:white;border:1px solid #818cf8;border-radius:6px;padding:4px 10px;font:14px Cairo,sans-serif;pointer-events:none';document.body.append(e);}});
  await expect(page.getByText(/^جارٍ تحميل/).first()).not.toBeVisible();
  if(region){const viewport=page.viewportSize()!;const height=await region.evaluate(e=>e.scrollHeight);if(height>viewport.height-40)await page.setViewportSize({width:viewport.width,height:Math.min(2600,height+80)});await region.scrollIntoViewIfNeeded();await region.screenshot({path:resolve(dir,file+'.png'),animations:'disabled'});await page.setViewportSize(viewport);}else {
   const fullPage=!['header-mobile','dashboard-mobile','service-year','statistics-groups','student-history','student-audit','call-history'].includes(file);
   if(fullPage){await page.evaluate(()=>scrollTo(0,0));await page.evaluate(()=>new Promise<void>(r=>requestAnimationFrame(()=>requestAnimationFrame(()=>r()))));}
   await page.screenshot({path:resolve(dir,file+'.png'),animations:'disabled',fullPage});
  }
  const png=readFileSync(resolve(dir,file+'.png'));
  captured[file]={route:page.url().split('#')[1] || '/login',kind:region ? 'dialog-or-section' : ['header-mobile','dashboard-mobile','service-year','statistics-groups','student-history','student-audit','call-history'].includes(file) ? 'viewport' : 'full-page',imageWidth:png.readUInt32BE(16),imageHeight:png.readUInt32BE(20)};
  console.log(app+' screenshot: '+file);
 };
 const dialog=()=>page.getByRole('dialog');
 const close=async()=>{await page.keyboard.press('Escape');await expect(dialog()).not.toBeVisible();};
 await go(app==='admin' ? 'stats' : 'dashboard');await shot('home');if(app==='admin'){await shot('statistics');await page.getByRole('heading',{name:'النشاط الأخير',exact:true}).scrollIntoViewIfNeeded();await shot('statistics-groups');}
 await page.setViewportSize({width:390,height:844});await page.evaluate(()=>scrollTo(0,0));await shot('header-mobile');if(app==='servant')await shot('dashboard-mobile');await page.setViewportSize({width:1440,height:1000});
 await page.getByRole('combobox',{name:'عام الخدمة',exact:true}).selectOption('2026');await shot('service-year');
 await go('profile');await shot('profile');await shot('profile-password',page.getByRole('heading',{name:'تغيير كلمة المرور',exact:true}).locator('..'));
 await go('students');await shot('students');await shot('student-filters');
 if(app==='servant'){await page.getByLabel('قائمة المخدومين').selectOption('Unassigned');await expect(page.getByText('مرقس — مثال غير مسند')).toBeVisible();await shot('unassigned');}
 await go('students/'+id);await expect(page.getByRole('heading',{name:'مينا سامح',exact:true})).toBeVisible();await shot('student-detail');
 await page.getByRole('button',{name:'تعديل البيانات',exact:true}).click();await shot('student-edit',dialog());await close();
 const history=page.locator('ydm-activity-history');await history.scrollIntoViewIfNeeded();await shot('student-history');await shot('call-history');
 await history.getByRole('button',{name:'تسجيل مكالمة',exact:true}).click();await dialog().getByLabel('ملاحظات',{exact:true}).fill('تم الاطمئنان عليه وتذكيره بموعد الاجتماع.');await shot('call-form',dialog());await close();
 await history.getByRole('button',{name:'تسجيل زيارة',exact:true}).click();await dialog().getByLabel('ملاحظات',{exact:true}).fill('زيارة بمشاركة الخدام للاطمئنان على الأسرة.');await shot('visit-form',dialog());
 await dialog().locator('ydm-resource-picker').getByRole('button').first().click();await page.getByRole('option',{name:'بطرس عادل',exact:true}).click();await shot('visit-participants',dialog());await close();
 await page.getByRole('heading',{name:'سجل تعديل البيانات',exact:true}).scrollIntoViewIfNeeded();await shot('student-audit');
 if(app==='servant'){await page.getByRole('button',{name:'طلب إزالة / إلغاء إسناد',exact:true}).click();await shot('removal-request',dialog());await close();}
 await go('activity-history');await shot('activity');await page.getByRole('button',{name:'تصحيح',exact:true}).first().click();await dialog().getByLabel('سبب التصحيح').fill('تصحيح وقت المكالمة بعد المراجعة.');await shot('activity-correct',dialog());await close();
 await page.getByRole('button',{name:'إلغاء السجل',exact:true}).first().click();await dialog().getByLabel('سبب إلغاء السجل').fill('تسجيل مكرر بالخطأ؛ يبقى الأصل محفوظًا.');await shot('activity-void',dialog());await close();
 await go('tasks');await shot('tasks');await page.getByRole('button',{name:'مهمة جديدة',exact:true}).click();await shot('task-new',dialog());await close();
 for(const [button,file] of [['إنجاز وتسجيل النشاط','task-complete'],['تغيير الموعد','task-reschedule'],['إلغاء','task-cancel']]){await page.getByRole('button',{name:button,exact:true}).first().click();await shot(file,dialog());await close();}
 if(app==='admin'){
  await page.getByRole('button',{name:'إعادة إسناد',exact:true}).first().click();await shot('task-reassign',dialog());await close();
  await page.locator('select[name="status"]').selectOption('PendingReview');await page.getByRole('button',{name:'تطبيق',exact:true}).click();await page.getByRole('button',{name:'مراجعة وتفعيل',exact:true}).click();await shot('task-activate',dialog());await close();
 }
 await go('requests?mine=true');await shot('requests');await page.getByRole('button',{name:app==='admin' ? 'مراجعة التفاصيل' : 'عرض التفاصيل',exact:true}).first().click();await shot('request-details',dialog());await close();
 await page.getByRole('button',{name:'سحب الطلب',exact:true}).first().click();await dialog().getByRole('textbox').fill('لم تعد هناك حاجة إلى هذا الطلب.');await shot('request-withdraw',dialog());await close();
 if(app==='admin'){for(const [button,file] of [['موافقة','request-approve'],['رفض','request-reject']]){await page.getByRole('button',{name:button,exact:true}).first().click();await shot(file,dialog());await close();}}
 await go('notifications');await shot('notifications');await page.getByRole('button',{name:'جدول جديد',exact:true}).click();await dialog().getByLabel('الجمعة',{exact:true}).check();await shot('notification-schedule',dialog());await close();
 await page.getByRole('button',{name:'تثبيت التطبيق',exact:true}).click();await shot('install',dialog());await close();
 await go('attendance');await page.getByRole('combobox',{name:'الاجتماع',exact:true}).selectOption(id);await expect(page.locator('.ydm-roster-row')).toHaveCount(2);await shot('attendance');
 if(app==='admin'){
  await page.getByRole('button',{name:'اعتماد الحضور',exact:true}).click();await shot('attendance-status',dialog());await close();
  await page.getByRole('button',{name:'إعداد يوم الاجتماع وأسبوع التقارير',exact:true}).click();await shot('attendance-settings',dialog());await close();
 }
 await context.route('**/api/meetings?**',r=>r.fulfill({json:{items:[{id,date:'2026-10-09',status:'Finalized',rosterKnown:true,version:'AAAAAAAABAE='}],totalCount:1,page:1,pageSize:25}}));
 await page.reload();await page.getByRole('combobox',{name:'الاجتماع',exact:true}).selectOption(id);await page.locator('.ydm-roster-row input').last().check();await page.getByRole('button',{name:'تسجيل حضور',exact:true}).click();await shot('attendance-mark',dialog());await close();
 if(app==='admin'){
  await context.route('**/api/meetings?**',r=>r.fulfill({json:{items:[{id,date:'2026-10-09',status:'Finalized',rosterKnown:false,version:'AAAAAAAABAE='}],totalCount:1,page:1,pageSize:25}}));
  await page.reload();await page.getByRole('combobox',{name:'الاجتماع',exact:true}).selectOption(id);await page.getByRole('button',{name:'مراجعة كشف المؤهلين التاريخي',exact:true}).click();await shot('attendance-eligibility',dialog());await close();
  await go('students');await page.getByRole('button',{name:'إضافة مخدوم',exact:true}).click();await dialog().getByLabel('الاسم الكامل *',{exact:true}).fill('مرقس — مثال إضافة');await dialog().getByLabel('رقم التليفون *',{exact:true}).fill('01012345678');await shot('student-add',dialog());await close();
  await page.getByRole('button',{name:'تعديل',exact:true}).first().click();await shot('student-edit-modal',dialog());await close();
  await page.getByRole('button',{name:/تخصيص/,exact:false}).first().click();await shot('student-assignment',dialog());await close();
  await page.getByRole('checkbox',{name:'تحديد الصفحة الحالية',exact:true}).check();
  for(const [button,file] of [['تخصيص المحددين لخادم','bulk-assignment'],['تغيير السنة الدراسية للمحددين','bulk-year'],['تغيير المنطقة للمحددين','bulk-area']]){await page.getByRole('button',{name:button,exact:true}).click();await shot(file,dialog());await close();}
  await page.getByRole('button',{name:'استيراد من ملف',exact:true}).click();await shot('import-dialog',dialog());
  await page.getByLabel('اختيار ملف CSV للاستيراد',{exact:true}).setInputFiles({name:'church-demo.csv',mimeType:'text/csv',buffer:Buffer.from('FullName,Phone,Area,AcademicYear\nمينا سامح,01012345678,شبرا البلد,سنة ثالثة\nصف يحتاج تصحيح,غيرصالح,بهتيم,سنة ثانية','utf8')});await expect(page.getByRole('dialog',{name:'معاينة ملف الاستيراد'})).toBeVisible();await shot('import-preview',dialog());
  const reviewed=dialog().getByRole('checkbox');if(await reviewed.count())await reviewed.check();await dialog().getByRole('button',{name:'استيراد الصفوف الصالحة',exact:true}).click();await expect(dialog()).not.toBeVisible();await shot('import-result');
  await page.getByRole('button',{name:'أرشفة',exact:true}).first().click();await shot('archive',dialog());await close();
  await go('archived-students');await shot('archives');await page.getByRole('button',{name:'استعادة',exact:true}).first().click();await shot('restore',dialog());await close();
  await go('servants');await shot('servants');await page.getByRole('button',{name:'إضافة خادم جديد',exact:true}).click();await shot('servant-add',dialog());
  await dialog().locator('input[type=file]').setInputFiles(resolve('AdminApp/public/logo.png'));const crop=page.getByRole('dialog',{name:'قص الصورة',exact:true});await expect(crop.getByRole('button',{name:'تأكيد',exact:true})).toBeEnabled();await shot('photo-crop',crop);await page.keyboard.press('Escape');await expect(crop).not.toBeVisible();await close();
  await page.getByRole('button',{name:'تعديل',exact:true}).first().click();await shot('servant-edit',dialog());await close();await page.getByRole('button',{name:'حذف',exact:true}).first().click();await shot('servant-delete',dialog());await close();
  await go('servants/'+owner);await shot('servant-detail');await page.getByRole('button',{name:'تغيير كلمة المرور',exact:true}).click();await shot('servant-reset',dialog());await close();
  await go('data-exports');await shot('data-exports');await go('operations');await shot('operations');
  for(const [path,file] of [['reports','report-catalog'],['reports/student-distribution','report-distribution'],['reports/servant-activity?period=week','report-activity'],['reports/students-no-contact','report-no-contact']]){await go(path);await shot(file);}
 }else{
  await go('request-add-student');await shot('add-request');
  await go('dashboard');for(const [button,file] of [['إرسال واتساب جماعي','whatsapp'],['إرسال SMS جماعي','sms']]){await page.getByRole('button',{name:button,exact:true}).click();await dialog().getByRole('textbox',{name:'الرسالة',exact:true}).fill('سلام ونعمة، ننتظركم في اجتماع الجمعة.');await shot(file,dialog());await close();}
 }
 for(const t of (app==='admin' ? adminTopics : servantTopics).filter(t=>t.report)){
  await go('report-suite?report='+t.report+'&dateFrom=2026-09-18&dateTo=2026-10-15');await expect(page.locator('ydm-report-table')).toBeVisible();await page.locator('ydm-report-table').scrollIntoViewIfNeeded();await shot('report-'+t.report);
  if(['servant-service-summary','pastoral-follow-up'].includes(t.report!)){const region=page.locator('.ydm-table-region');await region.evaluate(e=>e.scrollLeft=-e.scrollWidth);await shot('report-'+t.report+'-end');}
 }
 // Capture honest empty/error states, then restore normal fixtures for remaining screens.
 await context.route('**/api/reports/suite/contact?**',r=>r.fulfill({json:{title:'لا توجد نتائج مطابقة',period:'الفترة المختارة',scope:'مثال',columns:[],rows:[],totalCount:0,page:1,pageSize:25,totals:{roster:0,coverage:null}}}));await go('report-suite?report=contact');await shot('empty');
 await context.route('**/api/reports/suite/contact?**',r=>r.fulfill({status:503,json:{message:'تعذر تحميل البيانات. تحقق من الاتصال وأعد المحاولة.'}}));await page.reload();await expect(page.getByRole('alert')).toBeVisible();await shot('error');
 await go('notifications');await page.getByRole('button',{name:'جدول جديد',exact:true}).click();await dialog().getByRole('button',{name:'حفظ',exact:true}).click();await expect(dialog().getByRole('alert')).toBeVisible();await shot('validation',dialog());await close();
 // Onboarding screenshots are generated from separate fictional sessions, not from navigation around an account restriction.
 for(const [path,flags] of [['change-password',{requiresPasswordChange:true,requiresProfileCompletion:true}],['complete-profile',{requiresPasswordChange:false,requiresProfileCompletion:true}]] as const){
  await page.evaluate(({app,flags})=>{const key='ydm.'+(app==='admin' ? 'adminapp' : 'servantapp')+'.user',s=JSON.parse(localStorage.getItem(key)!);localStorage.setItem(key,JSON.stringify({...s,...flags}));},{app,flags});await page.goto(`/${app}/#/${path}`);await page.reload({waitUntil:'domcontentloaded'});await expect(page.locator('input')).not.toHaveCount(0);await shot(path);
 }
 await page.evaluate(app=>{localStorage.removeItem('ydm.'+(app==='admin' ? 'adminapp' : 'servantapp')+'.user');},app);await page.goto(`/${app}/#/login`);await page.reload({waitUntil:'domcontentloaded'});await expect(page.locator('input[type="password"]')).toBeVisible();await shot('login');
 const required=new Set((app==='admin' ? adminTopics : servantTopics).flatMap(t=>t.pictures.map(p=>p.file)));
 expect([...required].filter(file=>!captured[file]),'Every illustrated guide entry has a real capture').toEqual([]);
 writeFileSync(resolve(dir,'capture-manifest.json'),JSON.stringify({app,date:'2026-10-15',data:'Fictional demonstration records; all API requests intercepted locally',viewport:'1440x1000; mobile 390x844',captures:captured},null,2)+'\n');
});
