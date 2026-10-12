import {takeUntilDestroyed} from '@angular/core/rxjs-interop';
import { Component,DestroyRef,inject,signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { HttpClient,HttpParams } from '@angular/common/http';
import { Subscription } from 'rxjs';
import {ActivatedRoute,Router,RouterLink} from '@angular/router';
import {datetimeLocal,dateOnly} from '../core/utils';
import {reportDefinitions,pastoralReports} from '../core/report-catalog';
import { YDM_API,apiError,saveBlob,labels } from '../core/api';
import {YDM_SESSION} from '../core/api';
import {ResourcePickerComponent} from '../ui/resource-picker';
import { ReportTableComponent,Column } from '../ui/report-table';
interface Filter {days:number;minConsecutiveAbsences:number;dateFrom?:string;dateTo?:string;search?:string;area?:string;academicYear?:string;status?:string;issue?:string;type?:string;birthMonth?:number;servantId?:string;page?:number;pageSize?:number;}
interface Table {title:string;period:string;scope:string;columns:Column[];rows:Record<string,unknown>[];totalCount:number;page:number;pageSize:number;totals:Record<string,number|null>}
@Component({selector:'ydm-report-suite',standalone:true,imports:[FormsModule,RouterLink,ReportTableComponent,ResourcePickerComponent],template:`
  <section class="ydm-page" dir="rtl"><header class="content-header"><h1>{{reportName(activeReport)}}</h1><p class="text-muted">{{reportDescription(activeReport)}} التواريخ بتوقيت القاهرة؛ الإجماليات تشمل كل النتائج المطابقة.</p>@if(session().app==='Admin'){<a class="btn-outline" routerLink="/dashboard/reports">كل التقارير</a>}</header>
    <form class="glass-card ydm-panel ydm-filter-grid" (ngSubmit)="apply()">
      <label>التقرير<select class="form-input" aria-label="التقرير" name="report" [(ngModel)]="report" (ngModelChange)="reportChanged()">@for(group of groups;track group){<optgroup [label]="group">@for(r of reports;track r.key){@if(r.group===group){<option [value]="r.key">{{r.name}}</option>}}</optgroup>}</select></label>
      <label>من تاريخ<input class="form-input" type="date" name="from" [(ngModel)]="draft.dateFrom" /></label><label>إلى تاريخ (شامل)<input class="form-input" type="date" name="to" [(ngModel)]="draft.dateTo" /></label>
      <label>{{report==='servant-service-summary' || report.startsWith('servant-attendance') ? 'بحث باسم الخادم' : 'بحث باسم المخدوم أو الهاتف'}}<input class="form-input" name="search" [(ngModel)]="draft.search" /></label>@if(studentFilters()){<label>المنطقة<select class="form-input" name="area" [(ngModel)]="draft.area"><option value="">كل المناطق</option><option value="__missing__">منطقة غير مسجلة</option>@for(a of areas();track a){<option [value]="a">{{a}}</option>}</select></label><label>السنة الدراسية<select class="form-input" name="year" [(ngModel)]="draft.academicYear"><option value="">كل السنوات</option><option value="__missing__">سنة غير مسجلة</option>@for(y of years();track y){<option [value]="y">{{y}}</option>}</select></label><label>شهر الميلاد<select class="form-input" name="birthMonth" [(ngModel)]="draft.birthMonth"><option [ngValue]="undefined">الكل</option><option [ngValue]="0">ميلاد غير مسجل</option>@for(m of months;track m){<option [ngValue]="m">{{m}}</option>}</select></label>}
      @if(statuses().length){<label>الحالة<select class="form-input" name="status" [(ngModel)]="draft.status"><option value="">الكل</option>@for(s of statuses();track s){<option [value]="s">{{label(s)}}</option>}</select></label>}
      @if(issues().length){<label>{{report==='pastoral-follow-up' ? 'سبب المتابعة' : report.includes('attendance') ? 'المجموعة' : 'حالة البيانات'}}<select class="form-input" name="issue" [(ngModel)]="draft.issue"><option value="">الكل</option>@for(i of issues();track i.key){<option [value]="i.key">{{i.label}}</option>}</select></label>}
      @if(types().length){<label>النوع<select class="form-input" name="type" [(ngModel)]="draft.type"><option value="">الكل</option>@for(t of types();track t){<option [value]="t">{{label(t)}}</option>}</select></label>}
      @if(session().privileged){<ydm-resource-picker kind="report-actors" [label]="actorLabel()" [allowEmpty]="true" emptyLabel="كل الخدام" [value]="draft.servantId || null" (valueChange)="draft.servantId=$event || undefined"/>}
      <!-- Only relevant thresholds are shown, so attendance filters remain simple. -->
      @if(['contact','pastoral-follow-up','approvals'].includes(report)){<label>{{report==='approvals' ? 'الطلب قديم بعد عدد أيام' : 'لا يوجد تواصل حديث خلال عدد أيام'}}<input class="form-input" type="number" min="0" max="36500" name="days" [(ngModel)]="draft.days" required /></label>}
      @if(report==='pastoral-follow-up'){<label>حد الغياب المتتالي (اجتماعات)<input class="form-input" type="number" min="1" max="365" name="absences" [(ngModel)]="draft.minConsecutiveAbsences" required /></label>}
      <div class="ydm-toolbar"><button class="btn-primary" type="submit" [disabled]="loading() || exporting()">تطبيق الفلاتر</button><button class="btn-outline" type="button" (click)="reset()" [disabled]="loading()">مسح الفلاتر</button></div>
    </form>
    <p class="text-muted">{{dateMeaning()}}</p>
    @if(error()){<div class="ydm-notice text-error" role="alert">{{error()}} <button class="btn-outline" type="button" (click)="load()" [disabled]="loading()">إعادة المحاولة</button></div>}
    @if(loading()){<p class="ydm-notice" role="status">جارٍ تحميل النتائج…</p>}
    @if(table();as t){
      <div class="ydm-metrics" aria-label="إجماليات كل النتائج">@for(m of metrics();track m.key){<div class="glass-card ydm-metric"><strong>{{m.value===null ? '—' : m.value}}</strong><span>{{label(m.key)}}</span></div>}</div>
      <section class="glass-card ydm-panel" [attr.aria-busy]="loading()"><header class="ydm-toolbar"><div class="ydm-grow"><h2>{{t.title}}</h2><p class="text-muted">{{t.period}} · {{t.scope}} · {{t.totalCount}} نتيجة</p></div><button class="btn-outline" type="button" (click)="download('xlsx')" [disabled]="loading() || exporting()">Excel · كل النتائج</button><button class="btn-outline" type="button" (click)="download('csv')" [disabled]="loading() || exporting()">CSV · كل النتائج</button><button class="btn-outline" type="button" (click)="print()" [disabled]="loading() || exporting()">طباعة كل النتائج</button></header>
        @if(!loading() && t.rows.length){<ydm-report-table [columns]="t.columns" [rows]="t.rows" [label]="t.title" [links]="drilldown" [extraLinks]="extraDrilldown" />}
        @if(!loading() && !t.rows.length && !error()){<p class="ydm-notice" role="status">لا توجد نتائج مطابقة لهذه الفلاتر.</p>}
        <nav class="ydm-toolbar ydm-pager" aria-label="صفحات التقرير"><button class="btn-outline" type="button" (click)="page(-1)" [disabled]="loading() || t.page<=1">السابق</button><span>صفحة {{t.page}} · {{t.totalCount}} نتيجة</span><button class="btn-outline" type="button" (click)="page(1)" [disabled]="loading() || t.page*t.pageSize>=t.totalCount">التالي</button></nav>
      </section>
    }
  </section>`})
export class ReportSuitePage {
  private destroy=inject(DestroyRef);private http=inject(HttpClient);private api=inject(YDM_API);private request?:Subscription;session=inject(YDM_SESSION);areas=signal<string[]>([]);years=signal<string[]>([]);months=Array.from({length:12},(_,i)=>i+1);
  report='attendance';activeReport='attendance';draft:Filter={days:14,minConsecutiveAbsences:3};applied:Filter & {page:number;pageSize:number}={days:14,minConsecutiveAbsences:3,page:1,pageSize:25};
  loading=signal(false);exporting=signal(false);error=signal('');table=signal<Table|null>(null);label=(key:string)=>labels[key] || key;
  reports=reportDefinitions.filter(r=>!r.adminOnly || this.session().app==='Admin' && this.session().privileged);
  groups=['الحضور','خدمة الخدام والافتقاد','بيانات المخدومين','الطلبات'];
  private route=inject(ActivatedRoute);
  private router=inject(Router);
  constructor(){
    this.destroy.onDestroy(()=>this.request?.unsubscribe());
    this.http.get<string[]>(this.api+'/StudentQueries/areas').pipe(takeUntilDestroyed(this.destroy)).subscribe({next:r=>this.areas.set(r.filter(Boolean)),error:e=>this.error.set(apiError(e))});
    this.http.get<string[]>(this.api+'/StudentQueries/academic-years').pipe(takeUntilDestroyed(this.destroy)).subscribe({next:r=>this.years.set(r.filter(Boolean)),error:e=>this.error.set(apiError(e))});
    this.route.queryParamMap.pipe(takeUntilDestroyed(this.destroy)).subscribe(params=>{
      this.request?.unsubscribe();this.loading.set(false);this.table.set(null);
      const key=params.get('report') || 'attendance';this.report=key;this.activeReport=key;
      if(!this.reports.some(r=>r.key===key)){this.error.set('هذا التقرير غير متاح لهذا الحساب.');return;}
      this.report=key;this.activeReport=key;this.draft=this.defaults(key);
      for(const name of ['dateFrom','dateTo','servantId','area','academicYear','issue','status','type','search'] as const){const value=params.get(name);if(value)this.draft[name]=value;}
      for(const name of ['days','minConsecutiveAbsences','birthMonth'] as const){const value=params.get(name);if(value!==null)this.draft[name]=Number(value);}
      const problem=this.validation();if(problem){this.error.set(problem);return;}
      this.applied={...this.draft,page:1,pageSize:25};this.load();
    });
  }
  private defaults(key:string):Filter{const today=datetimeLocal(new Date()).slice(0,10);return {days:14,minConsecutiveAbsences:3,search:'',area:'',academicYear:'',status:'',issue:'',type:'',servantId:'',...(pastoralReports.has(key) ? {dateFrom:today.slice(0,8)+'01',dateTo:today} : {})};}
  reportName(key:string){return this.reports.find(r=>r.key===key)?.name || 'التقارير';}
  reportDescription(key:string){return this.reports.find(r=>r.key===key)?.description || '';}
  studentFilters(){return !this.report.startsWith('servant-attendance') && this.report!=='servant-service-summary';}
  actorLabel(){return this.report==='approvals' ? 'مقدم الطلب' : this.report==='contact' || this.report==='workload' || this.report==='pastoral-follow-up' ? 'خادم المجموعة الحالي' : this.report==='attendance' || this.report==='attendance-history' || this.report==='group-attendance' ? 'خادم المجموعة وقت الاجتماع' : this.report==='follow-up' ? 'الخادم المسؤول عن المتابعة' : 'الخادم';}
  drilldown=(row:Record<string,unknown>,column:string):{path:string;query?:Record<string,string>}|null=>{
    const id=String(row['id'] || '');if(!id)return null;
    const root=this.session().root;
    const query:Record<string,string>={};for(const key of ['dateFrom','dateTo','area','academicYear','search','birthMonth'] as const){const value=this.applied[key];if(value!==undefined && value!=='')query[key]=String(value);}
    if(this.activeReport==='servant-service-summary'){
      if(column==='name')return {path:root+'/servants/'+id};
      if(column==='assignedStudentsCount')return {path:root+'/students',query:{servantId:id}};
      const report=column==='eligible' || column==='present' || column==='rate' ? 'servant-attendance' : column==='openTasks' || column==='overdueTasks' ? 'follow-up' : column==='calls' || column==='visits' || column==='attempted' || column==='reached' ? undefined : null;
      if(report===null)return null;
      if(report==='follow-up')return {path:root+'/report-suite',query:{report,servantId:id,status:column==='overdueTasks' ? 'Overdue' : 'Open'}};
      return report ? {path:root+'/report-suite',query:{...query,report,servantId:id}} : {path:root+'/reports/servant-activity',query:{...query,servantId:id}};
    }
    if(this.activeReport==='group-attendance' && ['name','students','eligible','present','absent','rate'].includes(column))return {path:root+'/report-suite',query:{...query,report:'attendance',...(id==='00000000-0000-0000-0000-000000000000' ? {issue:'unassigned-group'} : {servantId:id})}};
    if(this.activeReport==='pastoral-follow-up' && column==='owner' && row['ownerId'])return {path:root+'/servants/'+row['ownerId']};
    if(column!=='name' && column!=='date')return null;
    if(['contact','workload','data-quality','pastoral-follow-up','attendance'].includes(this.activeReport))return {path:root+'/students/'+id};
    if(this.activeReport==='attendance-history' && row['personId'])return {path:root+'/students/'+row['personId']};
    if(this.activeReport==='follow-up')return {path:root+'/tasks',query:{taskId:id}};
    if(this.activeReport==='approvals')return {path:root+'/request-history',query:{requestId:id,kind:String(row['kind'])}};
    if(this.activeReport==='meeting-trends' && (this.session().privileged || this.session().role==='Secretary'))return {path:root+'/attendance',query:{date:String(row['date']).slice(0,10)}};
    return null;
  };
  extraDrilldown=(row:Record<string,unknown>,column:string)=>this.activeReport==='servant-service-summary' && column==='assignedStudentsCount' && row['id'] ? {label:'حضور المجموعة',path:this.session().root+'/report-suite',query:{report:'group-attendance',servantId:String(row['id']),...(this.applied.dateFrom ? {dateFrom:this.applied.dateFrom} : {}),...(this.applied.dateTo ? {dateTo:this.applied.dateTo} : {})}} : null;
  statuses(){return this.report==='follow-up' ? ['Open','Completed','Cancelled','PendingReview','Due','Overdue'] : this.report==='approvals' ? ['Pending','Approved','Rejected','Cancelled'] : [];}
  reportChanged(){this.draft.status='';this.draft.type='';this.draft.issue='';if(!this.studentFilters()){this.draft.area='';this.draft.academicYear='';this.draft.birthMonth=undefined;}if(pastoralReports.has(this.report) && !this.draft.dateFrom && !this.draft.dateTo){Object.assign(this.draft,this.defaults(this.report));}}
  types(){return this.report==='follow-up' ? ['Call','Visit'] : this.report==='approvals' ? ['addition','assignment','removal'] : [];}
  issues(){const map:Record<string,{key:string;label:string}[]>={attendance:[{key:'unassigned-group',label:'بلا خادم وقت الاجتماع'}],'attendance-history':[{key:'unassigned-group',label:'بلا خادم وقت الاجتماع'}],'group-attendance':[{key:'unassigned-group',label:'بلا خادم وقت الاجتماع'}],'pastoral-follow-up':[{key:'consecutive-absence',label:'غياب متتالي'},{key:'no-recent-success',label:'لا يوجد تواصل ناجح حديث'},{key:'never-attempted',label:'لا توجد محاولة افتقاد'},{key:'attendance-unknown',label:'بيانات حضور غير كافية'},{key:'overdue-tasks',label:'متابعة متأخرة'}],contact:[{key:'never-attempted',label:'لم تجرِ محاولة'},{key:'no-recent-attempt',label:'بلا محاولة حديثة'},{key:'no-recent-success',label:'بلا تواصل ناجح حديث'}],workload:[{key:'unassigned',label:'مخدومين غير مسندين'},{key:'inactive-owner',label:'مسؤول غير نشط'}],'data-quality':[{key:'phone',label:'هاتف ناقص أو غير صالح'},{key:'address',label:'عنوان ناقص'},{key:'area',label:'منطقة ناقصة'},{key:'year',label:'سنة ناقصة'},{key:'birthday',label:'ميلاد ناقص'},{key:'duplicates',label:'تشابه يحتاج مراجعة'}]};return map[this.report==='workload-owners' ? 'workload' : this.report] || [];}
  dateMeaning(){return this.activeReport==='servant-service-summary' ? 'النسبة هي مرات حضور الخادم ÷ الاجتماعات المعتمدة التي كان ضمن قائمتها. العمل يُنسب لمسجله أو مشارك الزيارة، حتى لو انتقل المخدوم إلى خادم آخر. المجموعة والمتابعات حسب الوضع الحالي، والزيارات الفعلية تُحسب مرة واحدة في الإجماليات.' : this.activeReport==='group-attendance' ? 'النسبة هي مرات الحضور ÷ فرص الحضور في القوائم المؤكدة. المجموعة بحسب خادم المخدوم وقت الاجتماع؛ قد يظهر المخدوم في مجموعتين عند نقله خلال الفترة.' : this.activeReport==='pastoral-follow-up' ? 'الغياب المتتالي داخل الفترة ومن الاجتماعات المعتمدة ذات القوائم المؤكدة فقط. آخر حضور وافتقاد حتى نهاية الفترة، وحد التواصل الحديث يُحسب من نهايتها. الخادم والمتابعات حسب الوضع الحالي. البيانات القديمة غير المؤكدة لا تعني غيابًا، ولا تُنشأ مهام تلقائيًا.' : this.activeReport==='meeting-trends' ? 'النسب من الاجتماعات المعتمدة ذات القوائم المؤكدة. فلاتر المنطقة والدراسة والميلاد والبحث تخص المخدومين؛ حضور الخدام يتأثر بالفترة والخادم فقط.' : this.activeReport==='workload-owners' ? 'القائمة بحسب التوزيع الحالي؛ الفترة تخص محاولات الافتقاد والتواصل الناجح مع المجموعة من أي خادم، وليست تقييمًا لعمل صاحب المجموعة وحده.' : this.activeReport==='follow-up' ? 'الفترة ترشح موعد استحقاق المهمة، بما فيها المهام المكتملة والملغاة. «تحتاج مراجعة» لا تدخل في المستحق والمتأخر.' : this.activeReport==='approvals' ? 'الفترة ترشح تاريخ تقديم الطلب، ويحسب زمن المعالجة حتى معالجته.' : this.activeReport==='contact' ? 'الفترة تخص تواريخ المكالمات والزيارات. آخر محاولة وآخر نجاح يعرضان التاريخ الكامل للمخدومين في القائمة الحالية.' : this.activeReport==='workload' || this.activeReport==='data-quality' ? 'يعرض هذا التقرير القائمة الحالية. فلاتر تاريخ النشاط لا تغير القائمة الحالية.' : 'الفترة تشمل تواريخ الاجتماعات. تحسب النسب من القوائم المؤكدة للاجتماعات المعتمدة فقط.';}
  metrics(){return Object.entries(this.table()?.totals || {}).map(([key,value])=>({key,value}));}
  private params(extra:Record<string,any>={}){let p=new HttpParams();for(const [key,value] of Object.entries({...this.applied,...extra}))if(value!=='' && value!==null && value!==undefined)p=p.set(key,String(value));return p;}
  private validation(){
    if(!Number.isInteger(this.draft.days) || this.draft.days<0 || this.draft.days>36500)return 'عدد الأيام يجب أن يكون بين 0 و36500.';
    if(!Number.isInteger(this.draft.minConsecutiveAbsences) || this.draft.minConsecutiveAbsences<1 || this.draft.minConsecutiveAbsences>365)return 'حد الغياب يجب أن يكون بين 1 و365 اجتماعًا.';
    if([this.draft.dateFrom,this.draft.dateTo].some(d=>d && dateOnly(d)!==d))return 'أدخل تاريخًا صحيحًا.';
    if(this.draft.dateFrom && this.draft.dateTo && this.draft.dateFrom>this.draft.dateTo)return 'يجب أن يسبق تاريخ البداية تاريخ النهاية.';
    return '';
  }
  apply(){
    if(this.loading() || this.exporting())return;const problem=this.validation();if(problem){this.error.set(problem);return;}
    const query:Record<string,string>={report:this.report};
    for(const [key,value] of Object.entries(this.draft))if(value!=='' && value!==null && value!==undefined && key!=='page' && key!=='pageSize' && (key!=='minConsecutiveAbsences' || this.report==='pastoral-follow-up') && (key!=='days' || ['contact','pastoral-follow-up','approvals'].includes(this.report)))query[key]=String(value);
    this.loading.set(true);
    this.router.navigate([],{relativeTo:this.route,queryParams:query,replaceUrl:true}).then(changed=>{
      if(!changed){this.activeReport=this.report;this.applied={...this.draft,page:1,pageSize:25};this.load();}
    }).catch(()=>{this.loading.set(false);this.error.set('تعذر تطبيق الفلاتر. احتفظنا بمدخلاتك؛ حاول مجددًا.');});
  }
  reset(){this.draft=this.defaults(this.report);this.apply();}
  page(delta:number){this.applied.page+=delta;this.load();}
  load(){if(!this.reports.some(r=>r.key===this.activeReport)){this.error.set('هذا التقرير غير متاح لهذا الحساب.');return;}this.request?.unsubscribe();this.loading.set(true);this.error.set('');this.request=this.http.get<Table>(`${this.api}/reports/suite/${this.activeReport}`,{params:this.params()}).subscribe({next:t=>{this.table.set(t);this.loading.set(false);},error:e=>{this.table.set(null);this.error.set(apiError(e));this.loading.set(false);}});}
  download(format:string){if(this.exporting() || this.loading())return;this.exporting.set(true);this.http.get(`${this.api}/reports/suite/${this.activeReport}/export`,{params:this.params({format}),responseType:'blob'}).pipe(takeUntilDestroyed(this.destroy)).subscribe({next:b=>{saveBlob(b,`${this.activeReport}.${format}`);this.exporting.set(false);},error:e=>{this.error.set(apiError(e));this.exporting.set(false);}});}
  print(){if(this.exporting() || this.loading())return;this.exporting.set(true);this.http.get<Table>(`${this.api}/reports/suite/${this.activeReport}`,{params:this.params({full:true})}).pipe(takeUntilDestroyed(this.destroy)).subscribe({next:t=>{this.table.set(t);this.exporting.set(false);requestAnimationFrame(()=>{const restore=()=>{window.removeEventListener("afterprint",restore);this.load();};window.addEventListener("afterprint",restore,{once:true});this.destroy.onDestroy(()=>window.removeEventListener("afterprint",restore));window.print();});},error:e=>{this.error.set(apiError(e));this.exporting.set(false);}});}
}
