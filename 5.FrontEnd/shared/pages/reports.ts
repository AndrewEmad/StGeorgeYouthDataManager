import {takeUntilDestroyed} from '@angular/core/rxjs-interop';
import { Component,DestroyRef,inject,signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { HttpClient,HttpParams } from '@angular/common/http';
import { Subscription } from 'rxjs';
import { YDM_API,apiError,saveBlob,labels } from '../core/api';
import {YDM_SESSION} from '../core/api';
import {ResourcePickerComponent} from '../ui/resource-picker';
import { ReportTableComponent,Column } from '../ui/report-table';
interface Filter {days:number;dateFrom?:string;dateTo?:string;search?:string;area?:string;academicYear?:string;status?:string;issue?:string;type?:string;birthMonth?:number;servantId?:string;page?:number;pageSize?:number;}
interface Table {title:string;period:string;scope:string;columns:Column[];rows:Record<string,unknown>[];totalCount:number;page:number;pageSize:number;totals:Record<string,number|null>}
@Component({selector:'ydm-report-suite',standalone:true,imports:[FormsModule,ReportTableComponent,ResourcePickerComponent],template:`
  <section class="ydm-page" dir="rtl"><header class="content-header"><h1>التقارير</h1><p class="text-muted">الحضور، المتابعة، التواصل، توزيع الخدمة، الموافقات وجودة البيانات. التواريخ بتوقيت القاهرة؛ الإجماليات تشمل كل النتائج المطابقة.</p></header>
    <form class="glass-card ydm-panel ydm-filter-grid" (ngSubmit)="apply()">
      <label>التقرير<select class="form-input" name="report" [(ngModel)]="report" (ngModelChange)="reportChanged()">@for(r of reports;track r.key){<option [value]="r.key">{{r.name}}</option>}</select></label>
      <label>من تاريخ<input class="form-input" type="date" name="from" [(ngModel)]="draft.dateFrom" /></label><label>إلى تاريخ (شامل)<input class="form-input" type="date" name="to" [(ngModel)]="draft.dateTo" /></label>
      <label>بحث بالاسم أو الهاتف<input class="form-input" name="search" [(ngModel)]="draft.search" /></label>@if(!report.startsWith('servant-attendance')){<label>المنطقة<select class="form-input" name="area" [(ngModel)]="draft.area"><option value="">كل المناطق</option><option value="__missing__">منطقة غير مسجلة</option>@for(a of areas();track a){<option [value]="a">{{a}}</option>}</select></label><label>السنة الدراسية<select class="form-input" name="year" [(ngModel)]="draft.academicYear"><option value="">كل السنوات</option><option value="__missing__">سنة غير مسجلة</option>@for(y of years();track y){<option [value]="y">{{y}}</option>}</select></label><label>شهر الميلاد<select class="form-input" name="birthMonth" [(ngModel)]="draft.birthMonth"><option [ngValue]="undefined">الكل</option><option [ngValue]="0">ميلاد غير مسجل</option>@for(m of months;track m){<option [ngValue]="m">{{m}}</option>}</select></label>}
      @if(statuses().length){<label>الحالة<select class="form-input" name="status" [(ngModel)]="draft.status"><option value="">الكل</option>@for(s of statuses();track s){<option [value]="s">{{label(s)}}</option>}</select></label>}
      @if(issues().length){<label>حالة البيانات<select class="form-input" name="issue" [(ngModel)]="draft.issue"><option value="">الكل</option>@for(i of issues();track i.key){<option [value]="i.key">{{i.label}}</option>}</select></label>}
      @if(types().length){<label>النوع<select class="form-input" name="type" [(ngModel)]="draft.type"><option value="">الكل</option>@for(t of types();track t){<option [value]="t">{{label(t)}}</option>}</select></label>}
      @if(session().privileged){<ydm-resource-picker kind="report-actors" label="المسؤول / مقدم الطلب / صاحب النشاط" [allowEmpty]="true" emptyLabel="كل المسؤولين" [value]="draft.servantId || null" (valueChange)="draft.servantId=$event || undefined"/>}
      <label>آخر عدد من الأيام<input class="form-input" type="number" min="0" max="36500" name="days" [(ngModel)]="draft.days" required /></label>
      <div class="ydm-toolbar"><button class="btn-primary" type="submit" [disabled]="loading() || exporting()">تطبيق الفلاتر</button><button class="btn-outline" type="button" (click)="reset()" [disabled]="loading()">مسح الفلاتر</button></div>
    </form>
    <p class="text-muted">{{dateMeaning()}}</p>
    @if(error()){<div class="ydm-notice text-error" role="alert">{{error()}} <button class="btn-outline" type="button" (click)="load()" [disabled]="loading()">إعادة المحاولة</button></div>}
    @if(loading()){<p class="ydm-notice" role="status">جارٍ تحميل النتائج…</p>}
    @if(table();as t){
      <div class="ydm-metrics" aria-label="إجماليات كل النتائج">@for(m of metrics();track m.key){<div class="glass-card ydm-metric"><strong>{{m.value===null ? '—' : m.value}}</strong><span>{{label(m.key)}}</span></div>}</div>
      <section class="glass-card ydm-panel" [attr.aria-busy]="loading()"><header class="ydm-toolbar"><div class="ydm-grow"><h2>{{t.title}}</h2><p class="text-muted">{{t.period}} · {{t.scope}} · {{t.totalCount}} نتيجة</p></div><button class="btn-outline" type="button" (click)="download('xlsx')" [disabled]="loading() || exporting()">Excel · كل النتائج</button><button class="btn-outline" type="button" (click)="download('csv')" [disabled]="loading() || exporting()">CSV · كل النتائج</button><button class="btn-outline" type="button" (click)="print()" [disabled]="loading() || exporting()">طباعة كل النتائج</button></header>
        @if(!loading() && t.rows.length){<ydm-report-table [columns]="t.columns" [rows]="t.rows" [label]="t.title" [links]="drilldown" />}
        @if(!loading() && !t.rows.length && !error()){<p class="ydm-notice" role="status">لا توجد نتائج مطابقة لهذه الفلاتر.</p>}
        <nav class="ydm-toolbar ydm-pager" aria-label="صفحات التقرير"><button class="btn-outline" type="button" (click)="page(-1)" [disabled]="loading() || t.page<=1">السابق</button><span>صفحة {{t.page}} · {{t.totalCount}} نتيجة</span><button class="btn-outline" type="button" (click)="page(1)" [disabled]="loading() || t.page*t.pageSize>=t.totalCount">التالي</button></nav>
      </section>
    }
  </section>`})
export class ReportSuitePage {
  private destroy=inject(DestroyRef);private http=inject(HttpClient);private api=inject(YDM_API);private request?:Subscription;session=inject(YDM_SESSION);areas=signal<string[]>([]);years=signal<string[]>([]);months=Array.from({length:12},(_,i)=>i+1);
  report='contact';activeReport='contact';draft:Filter={days:14};applied:Filter & {page:number;pageSize:number}={days:14,page:1,pageSize:25};
  loading=signal(false);exporting=signal(false);error=signal('');table=signal<Table|null>(null);label=(key:string)=>labels[key] || key;
  reports=[{key:'contact',name:'التواصل والتغطية'},{key:'follow-up',name:'مهام المتابعة'},{key:'workload',name:'الإسناد وعبء الخدمة'},{key:'workload-owners',name:'عبء الخدمة والتغطية لكل مسؤول'},{key:'approvals',name:'طلبات الموافقة'},{key:'data-quality',name:'جودة البيانات والتشابه'},{key:'attendance',name:'نسب حضور المخدومين والغياب المتتالي'},{key:'servant-attendance',name:'نسب حضور الخدام'},{key:'attendance-history',name:'سجل حضور المخدومين'},{key:'servant-attendance-history',name:'سجل حضور الخدام'},{key:'meeting-trends',name:'اتجاهات الاجتماعات'}];
  constructor(){const destroy=inject(DestroyRef);destroy.onDestroy(()=>this.request?.unsubscribe());this.http.get<string[]>(this.api+'/StudentQueries/areas').subscribe({next:r=>this.areas.set(r.filter(Boolean)),error:e=>this.error.set(apiError(e))});this.http.get<string[]>(this.api+'/StudentQueries/academic-years').subscribe({next:r=>this.years.set(r.filter(Boolean)),error:e=>this.error.set(apiError(e))});this.load();}
  drilldown=(row:Record<string,unknown>,column:string):{path:string;query?:Record<string,string>}|null=>{
    if(column!=='name' && column!=='date')return null;const id=String(row['id'] || '');if(!id)return null;
    const root=this.session().root;
    if(['contact','workload','data-quality'].includes(this.activeReport))return {path:root+'/students/'+id};
    if(this.activeReport==='follow-up')return {path:root+'/tasks',query:{taskId:id}};
    if(this.activeReport==='approvals')return {path:root+'/request-history',query:{requestId:id,kind:String(row['kind'])}};
    if(this.activeReport==='meeting-trends' && (this.session().privileged || this.session().role==='Secretary'))return {path:root+'/attendance',query:{date:String(row['date']).slice(0,10)}};
    return null;
  };
  statuses(){return this.report==='follow-up' ? ['Open','Completed','Cancelled','PendingReview','Due','Overdue'] : this.report==='approvals' ? ['Pending','Approved','Rejected','Cancelled'] : [];}
  reportChanged(){this.draft.status=undefined;this.draft.type=undefined;this.draft.issue=undefined;if(this.report.startsWith('servant-attendance')){this.draft.area=undefined;this.draft.academicYear=undefined;this.draft.birthMonth=undefined;}}
  types(){return this.report==='follow-up' ? ['Call','Visit'] : this.report==='approvals' ? ['addition','assignment','removal'] : [];}
  issues(){const map:Record<string,{key:string;label:string}[]>={contact:[{key:'never-attempted',label:'لم تجرِ محاولة'},{key:'no-recent-attempt',label:'بلا محاولة حديثة'},{key:'no-recent-success',label:'بلا تواصل ناجح حديث'}],workload:[{key:'unassigned',label:'مخدومين غير مسندين'},{key:'inactive-owner',label:'مسؤول غير نشط'}],'data-quality':[{key:'phone',label:'هاتف ناقص أو غير صالح'},{key:'address',label:'عنوان ناقص'},{key:'area',label:'منطقة ناقصة'},{key:'year',label:'سنة ناقصة'},{key:'birthday',label:'ميلاد ناقص'},{key:'duplicates',label:'تشابه يحتاج مراجعة'}]};return map[this.report==='workload-owners' ? 'workload' : this.report] || [];}
  dateMeaning(){return this.report==='follow-up' ? 'الفترة ترشح موعد استحقاق المهمة، بما فيها المهام المكتملة والملغاة. «تحتاج مراجعة» لا تدخل في المستحق والمتأخر.' : this.report==='approvals' ? 'الفترة ترشح تاريخ تقديم الطلب، ويحسب زمن المعالجة حتى معالجته.' : this.report==='contact' ? 'الفترة تخص تواريخ المكالمات والزيارات. آخر محاولة وآخر نجاح يعرضان التاريخ الكامل للمخدومين في القائمة الحالية.' : this.report==='workload' || this.report==='data-quality' ? 'يعرض هذا التقرير القائمة الحالية. فلاتر تاريخ النشاط لا تغير القائمة الحالية.' : 'الفترة تشمل تواريخ الاجتماعات. تحسب النسب من القوائم المؤكدة للاجتماعات المعتمدة فقط.';}
  metrics(){return Object.entries(this.table()?.totals || {}).map(([key,value])=>({key,value}));}
  private params(extra:Record<string,any>={}){let p=new HttpParams();for(const [key,value] of Object.entries({...this.applied,...extra}))if(value!=='' && value!==null && value!==undefined)p=p.set(key,String(value));return p;}
  apply(){if(this.loading() || this.exporting())return;if(!Number.isInteger(this.draft.days) || this.draft.days<0 || this.draft.days>36500){this.error.set('عدد الأيام يجب أن يكون بين 0 و36500.');return;}if(this.draft.dateFrom && this.draft.dateTo && this.draft.dateFrom>this.draft.dateTo){this.error.set('يجب أن يسبق تاريخ البداية تاريخ النهاية.');return;}this.activeReport=this.report;this.applied={...this.draft,page:1,pageSize:25};this.load();}
  reset(){this.draft={days:14};this.apply();}
  page(delta:number){this.applied.page+=delta;this.load();}
  load(){this.request?.unsubscribe();this.loading.set(true);this.error.set('');this.request=this.http.get<Table>(`${this.api}/reports/suite/${this.activeReport}`,{params:this.params()}).subscribe({next:t=>{this.table.set(t);this.loading.set(false);},error:e=>{this.table.set(null);this.error.set(apiError(e));this.loading.set(false);}});}
  download(format:string){if(this.exporting() || this.loading())return;this.exporting.set(true);this.http.get(`${this.api}/reports/suite/${this.activeReport}/export`,{params:this.params({format}),responseType:'blob'}).pipe(takeUntilDestroyed(this.destroy)).subscribe({next:b=>{saveBlob(b,`${this.activeReport}.${format}`);this.exporting.set(false);},error:e=>{this.error.set(apiError(e));this.exporting.set(false);}});}
  print(){if(this.exporting() || this.loading())return;this.exporting.set(true);this.http.get<Table>(`${this.api}/reports/suite/${this.activeReport}`,{params:this.params({full:true})}).pipe(takeUntilDestroyed(this.destroy)).subscribe({next:t=>{this.table.set(t);this.exporting.set(false);requestAnimationFrame(()=>{const restore=()=>{window.removeEventListener("afterprint",restore);this.load();};window.addEventListener("afterprint",restore,{once:true});this.destroy.onDestroy(()=>window.removeEventListener("afterprint",restore));window.print();});},error:e=>{this.error.set(apiError(e));this.exporting.set(false);}});}
}
