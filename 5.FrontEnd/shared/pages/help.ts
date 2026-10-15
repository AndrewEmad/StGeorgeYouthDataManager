import {afterNextRender,Component,computed,DestroyRef,inject,Injector,signal} from '@angular/core';
import {ActivatedRoute,RouterLink} from '@angular/router';
import {takeUntilDestroyed} from '@angular/core/rxjs-interop';
import {YDM_SESSION} from '../core/api';
import type {GuideTopic} from '../help/types';

const normalize=(s:string)=>s.normalize('NFKD').replace(/[\u064b-\u065f]/g,'').replace(/[أإآ]/g,'ا').replace(/ى/g,'ي').toLowerCase();
@Component({selector:'ydm-help',standalone:true,imports:[RouterLink],template:`
<section class="ydm-page help-page" dir="rtl">
 <header class="content-header"><h1>{{isAdmin ? 'دليل تطبيق الإدارة' : 'دليل تطبيق الخادم'}}</h1><p class="text-muted">شرح الصفحات والخطوات والتقارير بالصور · تحديث ١٥ أكتوبر ٢٠٢٦</p></header>
 <p class="ydm-notice">الصور أمثلة مستقلة من التطبيق ببيانات تجريبية؛ الأسماء والأرقام ليست بيانات الخدمة. قد تختلف البيانات والأزرار المتاحة بحسب مسؤوليتك.</p>
 <div class="ydm-toolbar help-controls"><label class="ydm-grow">ابحث في الدليل<input class="form-input" type="search" [value]="search()" (input)="search.set($any($event.target).value)" placeholder="مثل: حضور، إضافة، نسبة، استيراد" [disabled]="printing()" /></label><button class="btn-outline" type="button" (click)="search.set('')" [disabled]="!search() || printing()">مسح البحث</button><button class="btn-outline" type="button" (click)="printAll(false)" [disabled]="printing() || !active()">طباعة هذا الجزء</button><button class="btn-primary" type="button" (click)="printAll()" [disabled]="printing() || !topics().length">{{printing() ? 'جارٍ تجهيز الدليل…' : 'طباعة الدليل كاملًا'}}</button></div>
 <p class="text-muted">اختر جزءًا من الفهرس. اضغط الصورة لفتحها بحجمها الكامل. طباعة الجزء الحالي أسرع؛ تجهيز صور الدليل كاملًا قد يستغرق وقتًا. من نافذة الطباعة يمكنك اختيار «حفظ كملف PDF».</p>
 @if(error()){<p class="ydm-notice text-error" role="alert">{{error()}}</p>}
 @if(!topics().length && !error()){<p role="status">جارٍ فتح الدليل…</p>}
 @if(!printing() && topics().length && !matches().length){<p class="ydm-notice" role="status">لا توجد أجزاء مطابقة للبحث. جرّب كلمة أبسط أو امسح البحث.</p>}
 @if(printing() && printWhole()){<section class="help-print-index"><h2>فهرس الدليل</h2><ol>@for(t of topics();track t.id){<li>{{t.title}}</li>}</ol></section>}
 <div class="help-layout">
  <nav class="glass-card ydm-panel help-index" aria-label="فهرس دليل الاستخدام"><p role="status">{{matches().length}} جزءًا في الفهرس</p>@for(group of groups();track group){<h2>{{group}}</h2>@for(t of matches();track t.id){@if(t.group===group){<a class="help-index-link" [class.selected]="active()?.id===t.id" [attr.aria-current]="active()?.id===t.id ? 'page' : null" [routerLink]="[]" [queryParams]="{section:t.id}" queryParamsHandling="merge" (click)="focusTopic()">{{t.title}}</a>}}}</nav>
  <div class="help-content">@for(t of displayed();track t.id){
   <article class="glass-card ydm-panel help-article" [attr.id]="'guide-'+t.id">
    <h2 tabindex="-1" class="help-title">{{t.title}}</h2><p>{{t.intro}}</p>
    @if(t.path && canOpen(t)){<a class="btn-outline help-open" [routerLink]="pagePath(t)" [queryParams]="t.report ? {report:t.report} : {}">فتح الصفحة</a>}
    @if(t.steps.length){<h3>كيف أستخدمها؟</h3><ol>@for(s of t.steps;track $index){<li>{{s}}</li>}</ol>}
    @if(t.calculation?.length){<section class="help-calculation" aria-label="طريقة الحساب"><h3>ما معنى الأرقام وكيف تُحسب؟</h3><ul>@for(s of t.calculation;track $index){<li>{{s}}</li>}</ul>@if(t.example){<p class="ydm-notice"><strong>مثال:</strong> {{t.example}}</p>}</section>}
    @if(t.notes.length){<h3>ما الذي أحتاج معرفته؟</h3><ul>@for(s of t.notes;track $index){<li>{{s}}</li>}</ul>}
    @for(p of t.pictures;track p.file){<figure class="help-shot"><a [href]="imageUrl(p.file)" target="_blank" rel="noopener noreferrer" [attr.aria-label]="p.caption+'؛ فتح الصورة بحجمها الكامل'"><img [src]="imageUrl(p.file)" [alt]="p.caption" [attr.loading]="printing() ? 'eager' : 'lazy'" decoding="async" (error)="imageFailed(p.file)" /></a><figcaption>{{p.caption}} · مثال توضيحي</figcaption><ul>@for(point of p.points;track $index){<li>{{point}}</li>}</ul></figure>}
   </article>
  }</div>
 </div>
 @if(printing()){<button class="btn-outline help-return" type="button" (click)="printing.set(false)">العودة إلى قراءة الدليل</button>}
</section>`,styleUrl:'help.css'})
export class HelpPage {
 private route=inject(ActivatedRoute);private injector=inject(Injector);private destroy=inject(DestroyRef);
 session=inject(YDM_SESSION);isAdmin=this.session().app==='Admin';topics=signal<GuideTopic[]>([]);search=signal('');selected=signal('');printing=signal(false);printWhole=signal(true);error=signal('');
 matches=computed(()=>{const words=normalize(this.search().trim()).split(/\s+/).filter(Boolean);return this.topics().filter(t=>{const text=normalize([t.title,t.group,t.intro,...t.steps,...t.notes,...(t.calculation || []),t.example || ''].join(' '));return words.every(word=>text.includes(word));});});
 groups=computed(()=>[...new Set(this.matches().map(t=>t.group))]);active=computed<GuideTopic|undefined>(()=>this.matches().find(t=>t.id===this.selected()) || this.matches()[0]);displayed=computed(()=>this.printing() && this.printWhole() ? this.topics() : this.active() ? [this.active()!] : []);
 constructor(){
  this.route.queryParamMap.pipe(takeUntilDestroyed()).subscribe(p=>{this.selected.set(p.get('section') || '');this.focusTopic();});
  (this.isAdmin ? import('../help/admin') : import('../help/servant')).then(m=>{if(!this.destroy.destroyed)this.topics.set(m.topics);}).catch(()=>{if(!this.destroy.destroyed)this.error.set('تعذر فتح الدليل. أعد تحميل الصفحة عند توفر الاتصال.');});
  const done=()=>this.printing.set(false);window.addEventListener('afterprint',done);this.destroy.onDestroy(()=>window.removeEventListener('afterprint',done));
 }
 imageUrl(file:string){return new URL('assets/help/'+file+'.png',document.baseURI).href;}
 imageFailed(_file:string){if(!this.error())this.error.set('تعذر تحميل صورة توضيحية. أعد تحميل الصفحة عند توفر الاتصال؛ يمكنك متابعة قراءة الخطوات.');}
 pagePath(t:GuideTopic){return this.session().root+'/'+t.path;}
 canOpen(t:GuideTopic){return !t.attendanceDuty || this.isAdmin || ['Secretary','Manager'].includes(this.session().role || '');}
 focusTopic(){afterNextRender(()=>{const h=document.querySelector<HTMLElement>('.help-content .help-title');h?.focus({preventScroll:true});h?.scrollIntoView({block:'start',behavior:'instant'});},{injector:this.injector});}
 async printAll(all=true){
  if(this.printing() || !this.topics().length || (!all && !this.active()))return;this.error.set('');this.printWhole.set(all);this.printing.set(true);
  let timer:ReturnType<typeof setTimeout>|undefined;
  try{
   await new Promise<void>(resolve=>requestAnimationFrame(()=>requestAnimationFrame(()=>resolve())));
   const pictures=Array.from(document.querySelectorAll<HTMLImageElement>('.help-shot img'));
   await Promise.race([Promise.all([document.fonts.ready,...pictures.map(img=>img.decode())]),new Promise<never>((_,reject)=>{timer=setTimeout(()=>reject(new Error('Images not ready')),15000);})]);
   if(!this.destroy.destroyed && this.printing())window.print();
  }catch{this.printing.set(false);this.error.set('لم تكتمل صور الدليل. اتصل بالإنترنت وأعد محاولة الطباعة.');}
  finally{clearTimeout(timer);}
 }
}
