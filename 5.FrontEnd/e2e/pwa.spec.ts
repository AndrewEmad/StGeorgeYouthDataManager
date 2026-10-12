import {test,expect} from '@playwright/test';
import {authenticate} from './fixtures';

test.use({serviceWorkers:'allow'});
test('deployed app scopes cache the shell, report offline data and preserve a draft through an update',async({page,context,request})=>{
 test.setTimeout(120000);
 await authenticate(context,'admin');await authenticate(context,'servant');
 for(const app of ['admin','servant']){const logo=await request.get(`/YouthDataManager/${app}/logo.png`);expect(logo.ok()).toBeTruthy();expect(logo.headers()['content-type']).toContain('image/png');}
 await request.get('/__test/version?app=admin&version=0');
 await page.goto('/YouthDataManager/admin/#/dashboard/profile');
 await expect(page.getByLabel('الاسم الكامل',{exact:true})).toBeVisible();
 await page.evaluate(async()=>{await navigator.serviceWorker.register(new URL('ngsw-worker.js',document.baseURI).href,{scope:new URL('./',document.baseURI).href});await navigator.serviceWorker.ready;});
 await page.reload();await page.waitForFunction(()=>!!navigator.serviceWorker.controller);
 await page.getByLabel('الاسم الكامل',{exact:true}).fill('مسودة محفوظة في الصفحة');
 await request.get('/__test/version?app=admin&version=2');
 await page.evaluate(()=>navigator.serviceWorker.controller!.postMessage({action:'CHECK_FOR_UPDATES',nonce:34567}));
 await expect(page.getByText('تحديث جديد جاهز. احفظ أي مسودة قبل إعادة تحميل التطبيق.')).toBeVisible({timeout:30000});
 await expect(page.getByLabel('الاسم الكامل',{exact:true})).toHaveValue('مسودة محفوظة في الصفحة');
 await page.getByRole('button',{name:'لاحقًا'}).click();
 await expect(page.getByLabel('الاسم الكامل',{exact:true})).toHaveValue('مسودة محفوظة في الصفحة');
 const other=await context.newPage();await other.goto('/YouthDataManager/servant/#/tasks');
 await other.evaluate(async()=>{await navigator.serviceWorker.register(new URL('ngsw-worker.js',document.baseURI).href,{scope:new URL('./',document.baseURI).href});await navigator.serviceWorker.ready;});
 const scopes=await page.evaluate(async()=>(await navigator.serviceWorker.getRegistrations()).map(r=>new URL(r.scope).pathname));
 expect(scopes).toContain('/YouthDataManager/admin/');expect(scopes).toContain('/YouthDataManager/servant/');
 await context.setOffline(true);await page.reload();
 await expect(page.getByText('أنت غير متصل. واجهة التطبيق متاحة؛ البيانات والحفظ تحتاج اتصالًا.')).toBeVisible();
 await expect(page.getByRole('alert').first()).toBeVisible();
 await expect(page.getByText('تم حفظ بياناتك')).toHaveCount(0);
 await context.setOffline(false);await request.get('/__test/version?app=admin&version=0');
});
