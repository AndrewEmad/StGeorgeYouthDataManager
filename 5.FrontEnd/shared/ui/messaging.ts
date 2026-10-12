import {Component,DestroyRef,inject,input,output,signal} from '@angular/core';
import {FormsModule} from '@angular/forms';
import {HttpClient} from '@angular/common/http';
import {takeUntilDestroyed} from '@angular/core/rxjs-interop';
import {Subscription} from 'rxjs';
import {YDM_API,YDM_SESSION,apiError} from '../core/api';
import {phoneInternational} from '../core/utils';
import {ModalComponent} from './modal';
interface Recipient {id:string;name:string;phone:string;secondaryPhone?:string;}
@Component({selector:'ydm-messaging',standalone:true,imports:[FormsModule,ModalComponent],template:`
<app-modal [visible]="true" [title]="mode()==='sms' ? 'رسائل SMS للمخدومين' : 'رسائل واتساب للمخدومين'" (close)="close.emit()"><div body>
<p class="text-muted">يفتح الزر تطبيق الرسائل لكتابة الرسالة. راجع المستلم وأرسل الرسالة من التطبيق؛ فتحه لا يؤكد الإرسال.</p>
<label>الرسالة<textarea class="form-input" [(ngModel)]="message" maxlength="2000" rows="3"></textarea></label>
<label>بحث في المخدومين المسندين إليك<input class="form-input" [(ngModel)]="search" (ngModelChange)="searchChanged()" /></label>
@if(error()){<p class="text-error" role="alert">{{error()}} <button type="button" class="btn-outline" (click)="load()">إعادة المحاولة</button></p>}
@if(notice()){<p class="ydm-notice" role="status">{{notice()}}</p>}
@if(loading()){<p class="ydm-notice" role="status">جارٍ تحميل المستلمين…</p>}
@if(!loading() && !error()){
@if(mode()==='sms'){<label class="ydm-check"><input type="checkbox" [checked]="allPage()" [indeterminate]="somePage()" (change)="togglePage($event)" />تحديد هذه الصفحة فقط</label><p>{{selected().size}} مستلمًا محددًا <button type="button" class="btn-outline" (click)="clearSelection()">مسح التحديد</button></p>}
@for(r of rows();track r.id){<article class="ydm-history-row"><div class="ydm-grow"><strong>{{r.name}}</strong><p class="text-muted"><bdi dir="ltr">{{numbers(r).join(' · ') || 'رقم غير صالح'}}</bdi></p></div><div class="ydm-toolbar">
@for(number of numbers(r);track number){@if(mode()==='sms'){<label class="ydm-check"><input type="checkbox" [checked]="selected().get(r.id)===number" (change)="toggle(r,number,$event)" /><bdi dir="ltr">{{number}}</bdi></label>}
<a class="btn-outline" [href]="url(number)" [attr.target]="mode()==='whatsapp' ? '_blank' : null" rel="noopener noreferrer" (click)="composer(r.name)">فتح {{mode()==='sms' ? 'SMS' : 'واتساب'}}</a>}</div></article>}
@if(!rows().length){<p class="ydm-notice" role="status">لا يوجد مستلمون مطابقون.</p>}}
<nav class="ydm-toolbar ydm-pager" aria-label="صفحات المستلمين"><button type="button" class="btn-outline" [disabled]="loading() || page===1" (click)="changePage(-1)">السابق</button><span>صفحة {{page}} · {{total()}} مستلمًا</span><button type="button" class="btn-outline" [disabled]="loading() || page*25>=total()" (click)="changePage(1)">التالي</button></nav>
</div><div actions>@if(mode()==='sms' && android && selected().size){<a class="btn-primary" [href]="groupUrl()" (click)="composer('المستلمين المحددين')" [attr.aria-disabled]="loading()" [class.ydm-disabled]="loading()">فتح رسالة للمحدد (Android)</a>}<button type="button" class="btn-outline" (click)="close.emit()">إغلاق</button></div></app-modal>`})
export class MessagingComponent {
 mode=input<'sms'|'whatsapp'>('whatsapp');close=output<void>();private http=inject(HttpClient);private api=inject(YDM_API);private session=inject(YDM_SESSION);private destroy=inject(DestroyRef);private request?:Subscription;private timer?:ReturnType<typeof setTimeout>;
 rows=signal<Recipient[]>([]);total=signal(0);loading=signal(false);error=signal('');notice=signal('');selected=signal(new Map<string,string>());page=1;search='';message='';android=/Android/i.test(navigator.userAgent);
 constructor(){this.destroy.onDestroy(()=>{this.request?.unsubscribe();clearTimeout(this.timer);});this.load();}
 numbers(r:Recipient){return [...new Set([r.phone,r.secondaryPhone].map(p=>phoneInternational(p || '')).filter(Boolean).map(p=>'+'+p))];}
 load(){this.request?.unsubscribe();this.loading.set(true);this.error.set('');this.request=this.http.get<{items:Recipient[];totalCount:number}>(this.api+'/lookups/recipients',{params:{servantId:this.session().userId,search:this.search,page:this.page,pageSize:25}}).pipe(takeUntilDestroyed(this.destroy)).subscribe({next:r=>{this.rows.set(r.items);this.total.set(r.totalCount);this.loading.set(false);},error:e=>{this.rows.set([]);this.loading.set(false);this.error.set(apiError(e));}});}
 searchChanged(){clearTimeout(this.timer);this.request?.unsubscribe();this.rows.set([]);this.loading.set(true);this.timer=setTimeout(()=>{this.page=1;if(this.selected().size){this.selected.set(new Map());this.notice.set('تم مسح التحديد لأن نطاق البحث تغير.');}this.load();},300);}
 clearSelection(){this.selected.set(new Map());}
 changePage(delta:number){if(this.loading())return;this.page+=delta;this.load();}
 toggle(r:Recipient,number:string,event:Event){const checked=(event.target as HTMLInputElement).checked;this.selected.update(old=>{const next=new Map(old);if(checked)next.set(r.id,number);else next.delete(r.id);return next;});}
 allPage(){const valid=this.rows().filter(r=>this.numbers(r).length);return valid.length>0 && valid.every(r=>this.selected().has(r.id));}
 somePage(){return !this.allPage() && this.rows().some(r=>this.selected().has(r.id));}
 togglePage(event:Event){const checked=(event.target as HTMLInputElement).checked;this.selected.update(old=>{const next=new Map(old);for(const r of this.rows()){const number=this.numbers(r)[0];if(checked && number)next.set(r.id,number);else next.delete(r.id);}return next;});}
 url(number:string){const body=encodeURIComponent(this.message.trim());return this.mode()==='whatsapp' ? 'https://wa.me/'+number.slice(1)+(body ? '?text='+body : '') : 'sms:'+number+(body ? (/iPad|iPhone|iPod/.test(navigator.userAgent) ? '&body=' : '?body=')+body : '');}
 groupUrl(){const body=encodeURIComponent(this.message.trim());return 'sms:'+Array.from(new Set(this.selected().values())).join(',')+(body ? '?body='+body : '');}
 composer(name:string){this.notice.set('طُلب فتح تطبيق الرسائل لـ '+name+'. الإرسال يتم من التطبيق.');}
}
