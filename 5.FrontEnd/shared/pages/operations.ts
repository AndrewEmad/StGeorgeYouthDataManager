import {Component,DestroyRef,inject,signal} from '@angular/core';
import {HttpClient} from '@angular/common/http';
import {FormsModule} from '@angular/forms';
import {forkJoin,Subscription} from 'rxjs';
import {takeUntilDestroyed} from '@angular/core/rxjs-interop';
import {YDM_API,apiError,display} from '../core/api';
interface Delivery {id:string;title:string;status:string;availableAt:string;attempts:number;lastError:string|null;}
@Component({selector:'ydm-operations',standalone:true,imports:[FormsModule],template:`
<section class="ydm-page" dir="rtl"><header class="content-header"><h1>حالة التطبيق والإشعارات</h1><p class="text-muted">لمتابعة عمل التطبيق ومعرفة الإشعارات التي تحتاج إلى مراجعة. نجاح الإرسال لا يؤكد ظهور الإشعار على الهاتف.</p></header>
<button type="button" class="btn-outline" (click)="load()" [disabled]="loading()">تحديث الحالة</button>
@if(error()){<p class="ydm-notice text-error" role="alert">{{error()}}</p>}@if(message()){<p class="ydm-notice" role="status">{{message()}}</p>}
@if(loading()){<p class="ydm-notice" role="status">جارٍ تحميل الحالة…</p>}@else{
@if(metrics();as m){<div class="ydm-stat-grid">
<article class="glass-card ydm-panel"><h2>مشكلات التطبيق</h2><p>{{m.api.errors}}</p><p class="text-muted">عدد الطلبات التي انتهت بخطأ داخلي في التطبيق، منذ آخر تشغيل له.</p></article>
<article class="glass-card ydm-panel"><h2>عمليات بيانات بطيئة</h2><p>{{m.database.slowQueries}}</p><p class="text-muted">عدد عمليات التعامل مع البيانات التي استغرقت نصف ثانية أو أكثر، منذ آخر تشغيل للتطبيق.</p></article>
<article class="glass-card ydm-panel"><h2>إشعارات تحتاج متابعة</h2><p>{{m.failedDeliveries}}</p><p class="text-muted">عدد الإشعارات التي توقفت محاولات إرسالها وتحتاج إلى مراجعة. قد ينجح الإرسال لبعض الأجهزة فقط.</p></article>
<article class="glass-card ydm-panel"><h2>تأخر مراجعة التذكيرات (ثانية)</h2><p>{{formatLag(m.workerLagSeconds)}}</p><p class="text-muted">الوقت الذي لم يراجعه التطبيق بعد بحثًا عن تذكيرات مستحقة. قد يظهر رقم صغير بين المراجعات المعتادة.</p></article>
</div><p class="text-muted">آخر تحديث للأرقام {{format(m.capturedAt,'datetime')}} · تحديثات البيانات المطلوبة {{m.pendingMigrations}}</p><p class="text-muted">يبدأ عدّ مشكلات التطبيق وعمليات البيانات البطيئة من الصفر عند إعادة تشغيله. الإشعارات التي تحتاج متابعة تبقى مسجلة حتى تتغير حالتها.</p>}
<section class="glass-card ydm-panel"><form class="ydm-toolbar" (ngSubmit)="apply()"><label class="ydm-grow">حالة الإرسال<select class="form-input" name="status" [(ngModel)]="status"><option value="Failed">تحتاج متابعة</option><option value="Pending">قيد الإرسال</option><option value="Delivered">أرسله المزود</option><option value="Cancelled">ملغى</option><option value="">الكل</option></select></label><button type="submit" class="btn-outline">تطبيق</button></form>
@for(d of rows();track d.id){<article class="glass-card ydm-panel"><h3>{{d.title}}</h3><p>{{deliveryStatus(d.status)}} · {{format(d.availableAt,'datetime')}} · {{d.attempts}} محاولة</p>@if(d.lastError){<p class="text-error">{{d.lastError}}</p>}@if(d.status==='Failed'){<button type="button" class="btn-primary" (click)="retry(d)" [disabled]="busy().has(d.id)">{{busy().has(d.id) ? 'جارٍ الطلب…' : 'إعادة محاولة الإرسال'}}</button>}</article>}
@if(!rows().length){<p class="ydm-notice" role="status">لا توجد إشعارات مطابقة.</p>}
<nav class="ydm-toolbar ydm-pager" aria-label="صفحات الإشعارات"><button type="button" class="btn-outline" (click)="page(-1)" [disabled]="index<=1">السابق</button><span>صفحة {{index}} · {{total()}} إشعار</span><button type="button" class="btn-outline" (click)="page(1)" [disabled]="index*25>=total()">التالي</button></nav></section>}
</section>`})
export class OperationsPage {
private http=inject(HttpClient);private api=inject(YDM_API);private destroy=inject(DestroyRef);private request?:Subscription;
metrics=signal<any>(null);rows=signal<Delivery[]>([]);total=signal(0);loading=signal(false);error=signal('');message=signal('');busy=signal(new Set<string>());status='Failed';applied='Failed';index=1;format=display;
constructor(){this.load();this.destroy.onDestroy(()=>this.request?.unsubscribe());}
private readonly seconds=new Intl.NumberFormat('ar-EG',{maximumFractionDigits:0});
formatLag(value:number|null|undefined){return typeof value==='number' && Number.isFinite(value) ? this.seconds.format(Math.max(0,value)) : 'لا توجد مراجعة مسجلة';}
deliveryStatus(value:string){return ({Failed:'تحتاج متابعة',Pending:'قيد الإرسال',Delivered:'أرسله المزود',Cancelled:'ملغى'} as Record<string,string>)[value] || value;}
apply(){this.applied=this.status;this.index=1;this.load();}page(delta:number){this.index+=delta;this.load();}
load(){this.request?.unsubscribe();this.loading.set(true);this.error.set('');this.request=forkJoin({metrics:this.http.get<any>(`${this.api}/operations/metrics`),deliveries:this.http.get<{items:Delivery[];totalCount:number}>(`${this.api}/operations/deliveries`,{params:{...(this.applied ? {status:this.applied} : {}),page:this.index,pageSize:25}})}).subscribe({next:r=>{this.metrics.set(r.metrics);this.rows.set(r.deliveries.items);this.total.set(r.deliveries.totalCount);this.loading.set(false);},error:e=>{this.loading.set(false);this.rows.set([]);this.metrics.set(null);this.error.set(apiError(e));}});}
retry(delivery:Delivery){if(this.busy().has(delivery.id))return;this.busy.update(s=>new Set([...s,delivery.id]));this.message.set('');this.http.post(`${this.api}/operations/deliveries/${delivery.id}/retry`,{}).pipe(takeUntilDestroyed(this.destroy)).subscribe({next:()=>{this.release(delivery.id);this.message.set('تمت جدولة إعادة المحاولة. تابع الحالة للتأكد من النتيجة.');this.load();},error:e=>{this.release(delivery.id);this.error.set(apiError(e));}});}
private release(id:string){this.busy.update(s=>{const n=new Set(s);n.delete(id);return n;});}
}
