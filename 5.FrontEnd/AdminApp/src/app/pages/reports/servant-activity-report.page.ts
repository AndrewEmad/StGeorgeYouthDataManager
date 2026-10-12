import {Subscription} from 'rxjs';
import {ActivatedRoute} from '@angular/router';
import {DestroyRef} from '@angular/core';
import {takeUntilDestroyed} from '@angular/core/rxjs-interop';
import { Component, OnInit, signal, computed, inject, ChangeDetectionStrategy } from '@angular/core';
import { ReportsService } from '../../services/reports.service';
import { UsersService } from '../../services/users.service';
import { User } from '../../shared/models';
import { ServantActivitySummary } from '../../shared/models/report.model';
import { ReportViewerComponent } from '../../shared/components/report-viewer/report-viewer.component';
import { DataTableColumn } from '../../shared/components/data-table/data-table-column';
import { AdvancedFilterConfig, AdvancedFilterModel } from '../../shared/components/advanced-filter/advanced-filter-config';

@Component({
  selector: 'app-servant-activity-report-page',
  standalone: true,
  imports: [ReportViewerComponent],
  templateUrl: './servant-activity-report.page.html',
  styleUrls: ['./servant-follow-up.page.css'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ServantActivityReportPage implements OnInit {
  private request?:Subscription;ngOnDestroy(){this.request?.unsubscribe();}

  private route=inject(ActivatedRoute);
  private destroy=inject(DestroyRef);
  private reportsService = inject(ReportsService);
  private usersService = inject(UsersService);

  servants = signal<User[]>([]);
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
    const list = this.servants();
    return [
      { key: 'dateFrom', label: 'من تاريخ', type: 'date' },
      { key: 'dateTo', label: 'إلى تاريخ', type: 'date' },
      {
        key: 'servantId',
        label: 'الخادم',
        type: 'resource',resourceKind:'report-actors',
        placeholder: 'الكل',
        options: list.map((s) => ({ value: s.id ?? '', label: s.fullName ?? s.userName ?? '' })),
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
      const model:AdvancedFilterModel={};for(const key of ['dateFrom','dateTo','servantId']){const value=params.get(key);if(value)model[key]=value;}
      this.filterModel.set(model);this.page.set(1);this.loadData();
    });
  }

  onFilterChange(model: AdvancedFilterModel): void {
    this.filterModel.set(model);
    this.page.set(1);
    this.loadData();
  }

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

    this.request=this.reportsService.getServantActivitySummary(params).subscribe({
      next: (res) => {
        this.data.set(res.items ?? []);
        this.totalCount.set(res.totalCount ?? 0);
        this.page.set(res.page ?? this.page());
        this.loading.set(false);
      },
      error: () => {
        this.error.set('فشل تحميل التقرير');
        this.loading.set(false);
      },
    });
  }

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
