import {Component} from '@angular/core';
import {RouterLink} from '@angular/router';
import {reportDefinitions} from '@ydm/shared/core/report-catalog';

@Component({selector:'app-reports-catalog',standalone:true,imports:[RouterLink],template:`
  <section class="ydm-page" dir="rtl">
    <header class="content-header"><h1>تقارير خدمة الشباب</h1><p class="text-muted">لمتابعة حضور المخدومين والخدام والافتقاد. اختر تقريرًا، وحدد الفترة، ثم اعرضه أو صدّره.</p></header>
    @for(group of groups;track group.name){
      <section class="ydm-report-group" [attr.aria-label]="group.name"><h2>{{group.name}}</h2>
        <div class="ydm-report-cards">@for(report of group.reports;track report.key){
          <a class="glass-card ydm-report-card" [routerLink]="report.path" [queryParams]="report.query">
            <span class="material-icons" aria-hidden="true">{{report.icon}}</span><h3>{{report.name}}</h3><p class="text-muted">{{report.description}}</p>
          </a>
        }</div>
      </section>
    }
  </section>`})
export class ReportsCatalogPage {
  private reports=[...reportDefinitions.map(r=>({...r,path:'/dashboard/report-suite',query:{report:r.key} as Record<string,string>})),
    {key:'weekly-activity',name:'مكالمات وزيارات هذا الأسبوع',description:'الملخص الأسبوعي المسجل لكل خادم.',group:'خدمة الخدام والافتقاد',icon:'date_range',path:'/dashboard/reports/servant-performance',query:{}},
    {key:'activity',name:'نشاط الخدام خلال فترة',description:'عدد المكالمات والزيارات المسجلة لكل خادم خلال الفترة التي تختارها.',group:'خدمة الخدام والافتقاد',icon:'timeline',path:'/dashboard/reports/servant-activity',query:{}},
    {key:'no-contact',name:'مخدومون بلا محاولة افتقاد حديثة',description:'آخر محاولة تواصل للمخدومين الذين لم تُسجل لهم محاولة حديثة.',group:'خدمة الخدام والافتقاد',icon:'contact_phone',path:'/dashboard/reports/students-no-contact',query:{}},
    {key:'areas',name:'المخدومون حسب المنطقة',description:'توزيع المخدومين جغرافيًا مع فتح تفاصيل كل منطقة.',group:'بيانات المخدومين',icon:'map',path:'/dashboard/reports/students-by-area',query:{}},
    {key:'years',name:'المخدومون حسب السنة الدراسية',description:'توزيع المخدومين حسب سنوات الدراسة.',group:'بيانات المخدومين',icon:'school',path:'/dashboard/reports/students-by-year',query:{}},
    {key:'birthdays',name:'أعياد الميلاد',description:'توزيع أعياد ميلاد المخدومين حسب الشهر.',group:'بيانات المخدومين',icon:'cake',path:'/dashboard/reports/students-by-birth-month',query:{}}
  ];
  groups=['الحضور','خدمة الخدام والافتقاد','بيانات المخدومين','الطلبات'].map(name=>({name,reports:this.reports.filter(r=>r.group===name)}));
}
