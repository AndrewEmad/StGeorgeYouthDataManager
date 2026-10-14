import {Component,inject} from '@angular/core';
import {FormsModule} from '@angular/forms';
import {ServiceYearState} from '../core/service-year';
@Component({selector:'ydm-service-year',standalone:true,imports:[FormsModule],template:`
  <label class="ydm-year-label" for="global-service-year">عام الخدمة</label>
  <select dir="ltr" id="global-service-year" class="form-input ydm-year-select" aria-label="عام الخدمة" aria-describedby="service-year-description" [ngModel]="state.year()?.toString() ?? ''" [disabled]="state.pendingWrites()>0" (ngModelChange)="state.select($event)">
    <option value="">الكل</option>@for(year of years();track year){<option [value]="year">{{year}}–{{year+1}}</option>}
  </select>
  <span id="service-year-description" class="ydm-year-description" role="status">@if(state.period();as period){<bdi>{{period.dateFrom}}</bdi> إلى <bdi>{{period.dateTo}}</bdi>}@else{كل الأعوام} <span class="ydm-year-help">· قوائم المخدومين والخدام كاملة</span></span>
`,styles:[`:host{display:flex;align-items:center;gap:8px;min-inline-size:0;inline-size:100%;}.ydm-year-label{font-weight:600;white-space:nowrap;}.ydm-year-select{inline-size:146px;flex:none;min-block-size:44px;block-size:44px;padding-block:4px;line-height:1.2;direction:ltr;}.ydm-year-description{font-size:.8rem;color:var(--text-muted);min-inline-size:0;overflow-wrap:anywhere;}@media(max-width:600px){.ydm-year-description{position:absolute;inline-size:1px;block-size:1px;overflow:hidden;clip-path:inset(50%);}.ydm-year-select{flex:1;max-inline-size:180px;}}`]})
export class ServiceYearComponent {
 state=inject(ServiceYearState);
 years(){const today=new Intl.DateTimeFormat('en-CA',{timeZone:'Africa/Cairo',year:'numeric'}).format(new Date());const current=Number(today);const selected=this.state.year();const values=Array.from({length:current-2024},(_,i)=>current-i);return selected!==null && !values.includes(selected) ? [selected,...values] : values;}
}
