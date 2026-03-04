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
  private reportsService = inject(ReportsService);
  private usersService = inject(UsersService);

  servants = signal<User[]>([]);
  data = signal<ServantActivitySummary[]>([]);
  loading = signal(false);
  error = signal('');
  page = signal(1);
  pageSize = signal(10);
  totalCount = signal(0);
  pageSizeOptions = [5, 10, 20, 50];
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
        type: 'select',
        placeholder: 'الكل',
        options: list.map((s) => ({ value: s.id ?? '', label: s.fullName ?? s.userName ?? '' })),
      },
    ];
  });

  readonly columns: DataTableColumn<ServantActivitySummary>[] = [
    { key: 'fullName', header: 'الخادم', sortable: true },
    { key: 'assignedCount', header: 'عدد المخدومين المخصصين', sortable: true },
    { key: 'callsInPeriod', header: 'مكالمات في الفترة', sortable: true },
    { key: 'visitsInPeriod', header: 'زيارات في الفترة', sortable: true },
  ];

  ngOnInit(): void {
    this.usersService
      .getPaged({ page: 1, pageSize: 500, sortBy: 'fullName', sortDesc: false })
      .subscribe({
        next: (res) => {
          const sorted = (res.items ?? []).slice().sort((a, b) =>
            (a.fullName || '').localeCompare(b.fullName || '', 'ar')
          );
          this.servants.set(sorted);
        },
      });
    this.loadData();
  }

  onFilterChange(model: AdvancedFilterModel): void {
    this.filterModel.set(model);
    this.page.set(1);
    this.loadData();
  }

  loadData(): void {
    this.loading.set(true);
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

    this.reportsService.getServantActivitySummary(params).subscribe({
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
