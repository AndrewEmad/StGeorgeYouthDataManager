import { Component,DestroyRef,inject,signal } from '@angular/core';
import { SwUpdate } from '@angular/service-worker';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { PushService } from '../core/push';
@Component({selector:'ydm-pwa-status',standalone:true,template:`@if(update()){<div class="ydm-pwa-banner" role="status"><span>تحديث جديد جاهز. احفظ أي مسودة قبل إعادة تحميل التطبيق.</span><button class="btn-outline" type="button" (click)="reload()">حفظت عملي · إعادة تحميل</button><button class="btn-outline" type="button" (click)="update.set(false)">لاحقًا</button></div>}@if(offline()){<div class="ydm-pwa-banner" role="status">أنت غير متصل. واجهة التطبيق متاحة؛ البيانات والحفظ تحتاج اتصالًا.</div>}@if(push.foreground()){<div class="ydm-pwa-banner" role="status">{{push.foreground()}}<button class="btn-outline" type="button" (click)="push.foreground.set('')">إغلاق</button></div>}`})
export class PwaStatusComponent {
  private updates=inject(SwUpdate);push=inject(PushService);update=signal(false);offline=signal(!navigator.onLine);private destroy=inject(DestroyRef);
  constructor(){if(this.updates.isEnabled){this.updates.versionUpdates.pipe(takeUntilDestroyed()).subscribe(e=>{if(e.type==='VERSION_READY')this.update.set(true);});this.updates.unrecoverable.pipe(takeUntilDestroyed()).subscribe(()=>this.update.set(true));}const online=()=>this.offline.set(!navigator.onLine);window.addEventListener('online',online);window.addEventListener('offline',online);this.destroy.onDestroy(()=>{window.removeEventListener('online',online);window.removeEventListener('offline',online);});}
  reload(){location.reload();}
}
