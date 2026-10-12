import {Component,DestroyRef,inject,signal,computed,viewChild,TemplateRef,ChangeDetectionStrategy} from '@angular/core';
import {ActivatedRoute,Router,RouterLink} from '@angular/router';
import {FormsModule} from '@angular/forms';
import {Subscription} from 'rxjs';
import {takeUntilDestroyed} from '@angular/core/rxjs-interop';
import {ReportsService} from '../../services/reports.service';
import {StudentsByGroup} from '../../shared/models/report.model';
import {ReportViewerComponent} from '../../shared/components/report-viewer/report-viewer.component';
import {DataTableColumn} from '../../shared/components/data-table/data-table-column';
import {apiError} from '@ydm/shared/core/api';

@Component({selector:'app-student-distribution-page',standalone:true,imports:[RouterLink,FormsModule,ReportViewerComponent],templateUrl:'./student-distribution.page.html',styleUrls:['./servant-follow-up.page.css'],changeDetection:ChangeDetectionStrategy.OnPush})
export class StudentDistributionPage {
  private route=inject(ActivatedRoute);private router=inject(Router);private service=inject(ReportsService);private destroy=inject(DestroyRef);private request?:Subscription;
  group=signal('area');data=signal<StudentsByGroup[]>([]);loading=signal(false);error=signal('');page=signal(1);pageSize=signal(25);totalCount=signal(0);sortBy=signal<string|null>('groupKey');sortDesc=signal(false);pageSizeOptions=[25,50,100];
  groupTplRef=viewChild<TemplateRef<{$implicit:StudentsByGroup;value:unknown}>>('groupNameTpl');
  actionsTplRef=viewChild<TemplateRef<{$implicit:StudentsByGroup}>>('rowActionsTpl');
  groupLabel=computed(()=>this.group()==='area' ? 'المنطقة' : this.group()==='year' ? 'السنة الدراسية' : 'شهر الميلاد');
  columns=computed<DataTableColumn<StudentsByGroup>[]>(()=>[{key:'groupKey',header:this.groupLabel(),sortable:true,template:this.groupTplRef()},{key:'count',header:'عدد المخدومين',sortable:true}]);
  constructor(){this.destroy.onDestroy(()=>this.request?.unsubscribe());this.route.queryParamMap.pipe(takeUntilDestroyed(this.destroy)).subscribe(params=>{this.group.set(['area','year','birthMonth'].includes(params.get('group') || '') ? params.get('group')! : 'area');this.page.set(1);this.sortBy.set('groupKey');this.sortDesc.set(false);this.loadData();});}
  changeGroup(value:string){if(this.loading())return;void this.router.navigate([],{relativeTo:this.route,queryParams:{group:value}});}
  loadData(){this.request?.unsubscribe();this.loading.set(true);this.error.set('');const params={page:this.page(),pageSize:this.pageSize(),sortBy:this.sortBy() || undefined,sortDesc:this.sortDesc()};
    const request=this.group()==='area' ? this.service.getStudentsByArea(params) : this.group()==='year' ? this.service.getStudentsByAcademicYear(params) : this.service.getStudentsByBirthMonth(params);
    this.request=request.subscribe({next:r=>{this.data.set(r.items || []);this.totalCount.set(r.totalCount);this.loading.set(false);},error:e=>{this.data.set([]);this.error.set(apiError(e));this.loading.set(false);}});
  }
  groupName(value:string){if(this.group()==='birthMonth'){const month=Number(value);return month>=1 && month<=12 ? new Intl.DateTimeFormat('ar-EG',{month:'long',timeZone:'Africa/Cairo'}).format(new Date(Date.UTC(2000,month-1,15))) : 'شهر ميلاد غير مسجل';}return value || (this.group()==='area' ? 'منطقة غير مسجلة' : 'سنة دراسية غير مسجلة');}
  studentFilter(key:string){const value=key || '__missing__';return this.group()==='area' ? {area:value} : this.group()==='year' ? {academicYear:value} : {birthMonth:Number(key)>=1 && Number(key)<=12 ? key : '0'};}
  onPageChange(page:number){this.page.set(page);this.loadData();}
  onPageSizeChange(size:number){this.pageSize.set(Number(size));this.page.set(1);this.loadData();}
  onSortChange(event:{column:string;desc:boolean}){this.sortBy.set(event.column);this.sortDesc.set(event.desc);this.page.set(1);this.loadData();}
}
