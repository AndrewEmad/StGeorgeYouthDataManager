import {test,expect,type Page,type BrowserContext} from '@playwright/test';
import AxeBuilder from '@axe-core/playwright';
import {authenticate,fixtures,id,student,noOverflow} from './fixtures';

const second='33333333-3333-3333-3333-333333333333';
const currentVersion='AAAAAAAABAI=';
async function setup(page:Page,context:BrowserContext){
 await authenticate(context,'admin');await fixtures(context);
 await context.route('**/api/StudentQueries/paged?*',route=>{const index=Number(new URL(route.request().url()).searchParams.get('page'));return route.fulfill({json:{items:[{...student,id:index===1 ? id : second,fullName:index===1 ? 'مخدوم أول' : 'مخدوم ثان'}],totalCount:51,page:index,pageSize:25}});});
 await page.goto('/admin/#/dashboard/students');await page.getByRole('checkbox',{name:'تحديد مخدوم أول',exact:true}).check();
}
test('year bulk edit spans pages and prevents duplicate saving',async({page,context})=>{
 await setup(page,context);await page.getByRole('button',{name:'التالي',exact:true}).last().click();await page.getByRole('checkbox',{name:'تحديد مخدوم ثان',exact:true}).check();
 const submitted:any[]=[];let release!:()=>void;const gate=new Promise<void>(r=>release=r);
 await context.route('**/api/StudentCommands/bulk-academic-year',async route=>{submitted.push(route.request().postDataJSON());await gate;await route.fulfill({json:{outcomes:[{studentId:id,success:true,changed:true},{studentId:second,success:true,changed:true}]}});});
 await page.getByRole('button',{name:'تغيير السنة الدراسية للمحددين',exact:true}).click();const dialog=page.getByRole('dialog');
 await expect(dialog).toContainText('2');const year=dialog.getByLabel('السنة الدراسية الجديدة',{exact:true});await year.selectOption('سنة ثانية');
 const confirm=dialog.getByRole('button',{name:'تأكيد التغيير',exact:true});await confirm.click();await expect(year).toBeDisabled();await expect(dialog.getByRole('button',{name:'إلغاء',exact:true})).toBeDisabled();await expect.poll(()=>submitted.length).toBe(1);
 expect(submitted[0]).toEqual({students:[{studentId:id,version:student.version},{studentId:second,version:student.version}],academicYear:'سنة ثانية'});
 await expect(page.getByRole('checkbox',{name:'تحديد مخدوم ثان',exact:true})).toBeDisabled();await page.keyboard.press('Escape');await expect(dialog).toBeVisible();
 release();await expect(dialog).not.toBeVisible();await expect(page.getByText('تم حفظ السنة الدراسية «سنة ثانية» لـ 2 مخدوم.',{exact:true})).toBeVisible();await expect(page.getByText('تم حفظ السنة الدراسية «سنة ثانية» لـ 2 مخدوم.',{exact:true})).toBeFocused();await expect(page.getByText('تم حفظ السنة الدراسية «سنة ثانية» لـ 2 مخدوم.',{exact:true})).toBeInViewport();await expect(page.getByRole('button',{name:'تغيير السنة الدراسية للمحددين',exact:true})).toHaveCount(0);await expect(page.getByRole('checkbox',{name:'تحديد مخدوم أول',exact:true})).not.toBeChecked();
});
test('partial year edits keep only failures and require review of concurrent changes',async({page,context})=>{
 await setup(page,context);await page.getByRole('button',{name:'التالي',exact:true}).last().click();await page.getByRole('checkbox',{name:'تحديد مخدوم ثان',exact:true}).check();
 const submitted:any[]=[];await context.route('**/api/StudentCommands/bulk-academic-year',route=>{submitted.push(route.request().postDataJSON());return route.fulfill({json:{outcomes:submitted.length===1 ? [{studentId:id,success:true},{studentId:second,success:false,code:'Conflict',error:'تغيرت بيانات المخدوم.',fullName:'مخدوم ثان',academicYear:'سنة ثالثة',version:currentVersion}] : [{studentId:second,success:true}]}});});
 await page.getByRole('button',{name:'تغيير السنة الدراسية للمحددين',exact:true}).click();const dialog=page.getByRole('dialog');const year=dialog.getByLabel('السنة الدراسية الجديدة',{exact:true});await year.selectOption('حديث التخرج');await dialog.getByRole('button',{name:'تأكيد التغيير',exact:true}).click();
 await expect(page.getByText('1 مخدوم محدد',{exact:true})).toBeVisible();await expect(year).toHaveValue('حديث التخرج');await expect(dialog).toContainText('السنة المسجلة الآن: سنة ثالثة');await expect(dialog.getByRole('button',{name:'تأكيد التغيير',exact:true})).toBeDisabled();
 await dialog.getByRole('button',{name:'راجعت البيانات وأريد إعادة المحاولة',exact:true}).click();await dialog.getByRole('button',{name:'تأكيد التغيير',exact:true}).click();await expect(dialog).not.toBeVisible();
 expect(submitted).toHaveLength(2);expect(submitted[1]).toEqual({students:[{studentId:second,version:currentVersion}],academicYear:'حديث التخرج'});
});
test('failed year save preserves input and selection and allows safe retry',async({page,context})=>{
 await setup(page,context);let count=0;await context.route('**/api/StudentCommands/bulk-academic-year',route=>{count++;return count===1 ? route.fulfill({status:503,json:{message:'Service unavailable'}}) : route.fulfill({json:{outcomes:[{studentId:id,success:true}]}});});
 const trigger=page.getByRole('button',{name:'تغيير السنة الدراسية للمحددين',exact:true});await trigger.click();const dialog=page.getByRole('dialog');const year=dialog.getByLabel('السنة الدراسية الجديدة',{exact:true});const confirm=dialog.getByRole('button',{name:'تأكيد التغيير',exact:true});await expect(confirm).toBeDisabled();await expect(year.locator('option')).toHaveCount(10);await expect(confirm).toBeDisabled();expect(count).toBe(0);
 await year.selectOption('سنة ثالثة');await confirm.click();await expect(dialog.getByRole('alert')).toContainText('احتفظنا باختياراتك');await expect(year).toHaveValue('سنة ثالثة');await expect(page.getByText('1 مخدوم محدد',{exact:true})).toBeVisible();await confirm.click();await expect(dialog).not.toBeVisible();expect(count).toBe(2);
});

const sizes=[...[320,360,390,430,768,1024,1440,1920].map(width=>({width,height:900,zoom:false})),{width:844,height:390,zoom:false},{width:1024,height:360,zoom:false},{width:320,height:900,zoom:true}];
test('unavailable records can be removed from the retained group without trapping focus',async({page,context})=>{
 await setup(page,context);await context.route('**/api/StudentCommands/bulk-academic-year',route=>route.fulfill({json:{outcomes:[{studentId:id,success:false,code:'NotFound',error:'المخدوم غير موجود أو تمت أرشفته.'}]}}));
 await page.getByRole('button',{name:'تغيير السنة الدراسية للمحددين',exact:true}).click();const dialog=page.getByRole('dialog');await dialog.getByLabel('السنة الدراسية الجديدة',{exact:true}).selectOption('سنة ثانية');await dialog.getByRole('button',{name:'تأكيد التغيير',exact:true}).click();
 await expect(dialog).toContainText('المخدوم غير موجود أو تمت أرشفته.');await dialog.getByRole('button',{name:'إلغاء اختيار مخدوم أول',exact:true}).click();await expect(dialog).not.toBeVisible();await expect(page.getByText('تم إلغاء الاختيارات المتبقية.',{exact:true})).toBeFocused();await expect(page.getByText('تم إلغاء الاختيارات المتبقية.',{exact:true})).toBeInViewport();await expect(page.getByRole('checkbox',{name:'تحديد مخدوم أول',exact:true})).not.toBeChecked();
});
for(const size of sizes)test(`bulk year dialog fits ${size.width}x${size.height}${size.zoom ? ' reflow' : ''}`,async({page,context},info)=>{
 await page.setViewportSize(size);await setup(page,context);if(size.zoom)await page.addStyleTag({content:'html{font-size:200% !important}'});
 expect((await noOverflow(page)).scroll).toBeLessThanOrEqual(size.width+1);
 const trigger=page.getByRole('button',{name:'تغيير السنة الدراسية للمحددين',exact:true});await trigger.click();const dialog=page.getByRole('dialog');await expect(dialog).toBeVisible();const bounds=await dialog.boundingBox();expect(bounds!.width).toBeLessThanOrEqual(size.width);expect(bounds!.height).toBeLessThanOrEqual(size.height);
 const year=dialog.getByLabel('السنة الدراسية الجديدة',{exact:true});await year.selectOption('سنة سابعة');const confirm=dialog.getByRole('button',{name:'تأكيد التغيير',exact:true});await confirm.scrollIntoViewIfNeeded();await expect(confirm).toBeInViewport();expect((await noOverflow(page)).scroll).toBeLessThanOrEqual(size.width+1);
 expect((await new AxeBuilder({page}).include('dialog[open]').withTags(['wcag2a','wcag2aa','wcag21aa']).analyze()).violations.map(v=>({id:v.id,nodes:v.nodes.map(n=>n.target)}))).toEqual([]);
 if(size.width===390 || size.width===1440)await page.screenshot({path:info.outputPath('bulk-academic-year-dialog.png')});
 await page.keyboard.press('Escape');await expect(dialog).not.toBeVisible();await expect(trigger).toBeFocused();
});
