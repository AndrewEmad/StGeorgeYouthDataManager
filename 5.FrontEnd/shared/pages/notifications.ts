import { Component,DestroyRef,inject,signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { FormsModule } from '@angular/forms';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { YDM_API,apiError } from '../core/api';
import { PushService } from '../core/push';
import { ModalComponent } from '../ui/modal';
interface Schedule {id:string;daysOfWeek:string;timeOfDay:string;isActive:boolean;}
@Component({selector:'ydm-notifications',standalone:true,imports:[FormsModule,ModalComponent],template:`
  <section class="ydm-page" dir="rtl"><header class="content-header"><h1>الإشعارات والتذكيرات</h1><p class="text-muted">تذكيرات بالمهام المفتوحة المستحقة. مواعيد التذكير بتوقيت القاهرة.</p></header>
    <section class="glass-card ydm-panel"><h2>إشعارات هذا الجهاز</h2><p>{{push.registered() ? 'الجهاز مسجل' : 'الجهاز غير مسجل بعد'}}</p><button class="btn-primary" type="button" (click)="push.sync()" [disabled]="push.busy()">{{push.busy() ? 'جارٍ التسجيل…' : 'تفعيل / تحديث إشعارات الجهاز'}}</button>@if(push.message()){<p class="ydm-notice" role="status">{{push.message()}}</p>}</section>
    <section class="glass-card ydm-panel"><header class="ydm-toolbar"><h2 class="ydm-grow">جداول التذكير</h2><button class="btn-primary" type="button" (click)="edit()" [disabled]="loading()">جدول جديد</button></header>
      @if(loading()){<p role="status">جارٍ تحميل الجداول…</p>}@if(error()){<p class="text-error" role="alert">{{error()}} <button class="btn-outline" type="button" (click)="load()">إعادة المحاولة</button></p>}
      @if(!loading()){@for(s of schedules();track s.id){<article class="ydm-panel"><p>{{daysLabel(s)}} · <bdi>{{s.timeOfDay}}</bdi> · {{s.isActive ? 'نشط' : 'متوقف'}}</p><div class="ydm-toolbar"><button class="btn-outline" type="button" (click)="edit(s)" [disabled]="busy().has(s.id)">تعديل</button><button class="btn-outline" type="button" (click)="toggle(s)" [disabled]="busy().has(s.id)">{{s.isActive ? 'إيقاف' : 'استئناف'}}</button><button class="btn-outline danger" type="button" (click)="remove(s)" [disabled]="busy().has(s.id)">حذف</button></div></article>}@if(!schedules().length && !error()){<p role="status">لم تُضف جداول تذكير بعد.</p>}}
    </section>
    <app-modal [visible]="opened()" [title]="editing ? 'تعديل جدول التذكير' : 'جدول تذكير جديد'" [busy]="saving()" (close)="opened.set(false)"><div body><label>وقت التذكير<input class="form-input" type="time" [(ngModel)]="time" [disabled]="saving()" required /></label><fieldset class="ydm-fieldset" [disabled]="saving()"><legend>أيام التذكير (اختر يومًا واحدًا على الأقل)</legend>@for(day of dayNames;track $index){<label class="ydm-check"><input type="checkbox" [checked]="days.has($index)" (change)="selectDay($index,$any($event.target).checked)"/>{{day}}</label>}</fieldset>@if(dialogError()){<p class="text-error" role="alert">{{dialogError()}}</p>}</div><div actions><button class="btn-outline" type="button" (click)="opened.set(false)" [disabled]="saving()">إغلاق</button><button class="btn-primary" type="button" (click)="save()" [disabled]="saving()">{{saving() ? 'جارٍ الحفظ…' : 'حفظ'}}</button></div></app-modal>
  </section>`})
export class NotificationsPage {
  push=inject(PushService);private http=inject(HttpClient);private api=inject(YDM_API);private destroy=inject(DestroyRef);
  schedules=signal<Schedule[]>([]);loading=signal(false);error=signal('');opened=signal(false);saving=signal(false);dialogError=signal('');busy=signal(new Set<string>());editing:Schedule|null=null;time='09:00';days=new Set<number>();dayNames=['الأحد','الإثنين','الثلاثاء','الأربعاء','الخميس','الجمعة','السبت'];
  constructor(){this.load();}
  load(){if(this.loading())return;this.loading.set(true);this.error.set('');this.http.get<Schedule[]>(`${this.api}/Notification/schedules`).pipe(takeUntilDestroyed(this.destroy)).subscribe({next:r=>{this.schedules.set(r);this.loading.set(false);},error:e=>{this.loading.set(false);this.error.set(apiError(e));}});}
  daysLabel(s:Schedule){return s.daysOfWeek.split(',').map(d=>this.dayNames[Number(d)]).join('، ');}
  edit(s?:Schedule){this.editing=s || null;this.time=s?.timeOfDay.slice(0,5) || '09:00';this.days=new Set(s?.daysOfWeek.split(',').map(Number) || []);this.dialogError.set('');this.opened.set(true);}
  selectDay(day:number,checked:boolean){checked ? this.days.add(day) : this.days.delete(day);}
  save(){if(this.saving())return;if(!this.days.size || !/^([01]\d|2[0-3]):[0-5]\d$/.test(this.time)){this.dialogError.set('اختر يومًا على الأقل ووقتًا صالحًا.');return;}this.dialogError.set('');this.saving.set(true);const body={daysOfWeek:[...this.days].sort().join(','),timeOfDay:this.time,isActive:this.editing?.isActive ?? true};const req=this.editing ? this.http.put(`${this.api}/Notification/schedules/${this.editing.id}`,body) : this.http.post(`${this.api}/Notification/schedules`,body);req.pipe(takeUntilDestroyed(this.destroy)).subscribe({next:()=>{this.saving.set(false);this.opened.set(false);this.load();},error:e=>{this.saving.set(false);this.dialogError.set(apiError(e));}});}
  toggle(s:Schedule){this.mutate(s,this.http.put(`${this.api}/Notification/schedules/${s.id}`,{...s,isActive:!s.isActive}));}
  remove(s:Schedule){this.mutate(s,this.http.delete(`${this.api}/Notification/schedules/${s.id}`));}
  private mutate(s:Schedule,request:import('rxjs').Observable<unknown>){if(this.busy().has(s.id))return;this.busy.update(v=>new Set([...v,s.id]));request.pipe(takeUntilDestroyed(this.destroy)).subscribe({next:()=>{this.finish(s.id);this.load();},error:e=>{this.finish(s.id);this.error.set(apiError(e));}});}
  private finish(id:string){this.busy.update(v=>{const next=new Set(v);next.delete(id);return next;});}
}
