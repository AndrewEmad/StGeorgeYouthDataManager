import {Subscription,map} from 'rxjs';
import {FormsModule} from '@angular/forms';
import {Router,RouterLink} from '@angular/router';
import {ActivatedRoute} from '@angular/router';
import {DestroyRef} from '@angular/core';
import {takeUntilDestroyed} from '@angular/core/rxjs-interop';
import { Component, OnInit, signal, computed, inject, ChangeDetectionStrategy } from '@angular/core';
import { ReportsService } from '../../services/reports.service';
import { ServantActivitySummary } from '../../shared/models/report.model';
import { ReportViewerComponent } from '../../shared/components/report-viewer/report-viewer.component';
import { DataTableColumn } from '../../shared/components/data-table/data-table-column';
import { AdvancedFilterConfig, AdvancedFilterModel } from '../../shared/components/advanced-filter/advanced-filter-config';

@Component({
  selector: 'app-servant-activity-report-page',
  standalone: true,
  imports: [ReportViewerComponent,FormsModule,RouterLink],
  templateUrl: './servant-activity-report.page.html',
  styleUrls: ['./servant-follow-up.page.css'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ServantActivityReportPage implements OnInit {
  private request?:Subscription;ngOnDestroy(){this.request?.unsubscribe();}

  private route=inject(ActivatedRoute);private router=inject(Router);period=signal('custom');
  private destroy=inject(DestroyRef);
  private reportsService = inject(ReportsService);

  data = signal<ServantActivitySummary[]>([]);
  loading = signal(false);
  error = signal('');
  page = signal(1);
  pageSize = signal(25);
  totalCount = signal(0);
  pageSizeOptions = [25, 50, 100];
  sortBy = signal<string | null>('fullName');
  sortDesc = signal(false);
  filterModel = signal<AdvancedFilterModel>({});

  filterConfig = computed<AdvancedFilterConfig>(() => {
    if(this.period()==='week')return [];
    return [
      { key: 'dateFrom', label: 'من تاريخ', type: 'date' },
      { key: 'dateTo', label: 'إلى تاريخ', type: 'date' },
      {
        key: 'servantId',
        label: 'الخادم',
        type: 'resource',resourceKind:'report-actors',
        placeholder: 'الكل',
      },
    ];
  });

  readonly columns: DataTableColumn<ServantActivitySummary>[] = [
    { key: 'fullName', header: 'الخادم', sortable: true },
    { key: 'assignedCount', header: 'مخدوموه الحاليون', sortable: true },
    { key: 'callsInPeriod', header: 'مكالمات في الفترة', sortable: true },
    { key: 'visitsInPeriod', header: 'زيارات شارك فيها خلال الفترة', sortable: true },
  ];

  ngOnInit(): void {
    this.route.queryParamMap.pipe(takeUntilDestroyed(this.destroy)).subscribe(params=>{
      this.period.set(params.get('period')==='week' ? 'week' : 'custom');const model:AdvancedFilterModel={};for(const key of ['dateFrom','dateTo','servantId']){const value=params.get(key);if(value)model[key]=value;}
      this.filterModel.set(model);this.page.set(1);this.loadData();
    });
  }

  onFilterChange(model: AdvancedFilterModel): void {
    void this.router.navigate([],{relativeTo:this.route,queryParams:{period:'custom',...Object.fromEntries(Object.entries(model).filter(([,v])=>v!==null && v!==''))}});
  }

  changePeriod(period:string){if(this.loading())return;void this.router.navigate([],{relativeTo:this.route,queryParams:{period,...(period==='custom' ? this.filterModel() : {})}});}

  loadData(): void {
    this.request?.unsubscribe();this.loading.set(true);
    this.error.set('');
    const model = this.filterModel();
    const params: {
      dateFrom?: string;
      dateTo?: string;
      servantId?: string;
      page: number;
      pageSize: number;
      sortBy?: string;
      sortDesc?: boolean;
    } = {
      page: this.page(),
      pageSize: this.pageSize(),
      sortBy: this.sortBy() ?? undefined,
      sortDesc: this.sortDesc(),
    };
    const dateFrom = model['dateFrom'];
    const dateTo = model['dateTo'];
    const servantId = model['servantId'];
    if (dateFrom != null && String(dateFrom).trim()) params.dateFrom = String(dateFrom).trim();
    if (dateTo != null && String(dateTo).trim()) params.dateTo = String(dateTo).trim();
    if (servantId != null && String(servantId).trim()) params.servantId = String(servantId).trim();

    const weekSort:Record<string,string>={assignedCount:'assignedStudentsCount',callsInPeriod:'callsThisWeek',visitsInPeriod:'visitsThisWeek'};
    const request=this.period()==='week' ? this.reportsService.getServantPerformancesPaged({page:params.page,pageSize:params.pageSize,sortBy:weekSort[params.sortBy || ''] || params.sortBy,sortDesc:params.sortDesc}).pipe(map(r=>({...r,items:r.items.map(x=>({servantId:x.servantId,fullName:x.fullName,assignedCount:x.assignedStudentsCount,callsInPeriod:x.callsThisWeek,visitsInPeriod:x.visitsThisWeek}))}))) : this.reportsService.getServantActivitySummary(params);
    this.request=request.subscribe({
      next: (res) => {
        this.data.set(res.items ?? []);
        this.totalCount.set(res.totalCount ?? 0);
        this.page.set(res.page ?? this.page());
        this.loading.set(false);
      },
      error: () => {
        this.data.set([]);this.error.set('فشل تحميل التقرير');
        this.loading.set(false);
      },
    });
  }

  onRowClick(row:ServantActivitySummary){void this.router.navigate(['/dashboard/servants',row.servantId]);}

  onPageChange(p: number): void {
    this.page.set(p);
    this.loadData();
  }

  onPageSizeChange(n: number): void {
    this.pageSize.set(Number(n));
    this.page.set(1);
    this.loadData();
  }

  onSortChange(event: { column: string; desc: boolean }): void {
    this.sortBy.set(event.column);
    this.sortDesc.set(event.desc);
    this.page.set(1);
    this.loadData();
  }
}
