import { Component, OnInit, signal, computed, inject, ChangeDetectionStrategy, viewChild, TemplateRef } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ReportsService } from '../../services/reports.service';
import { UsersService } from '../../services/users.service';
import { User } from '../../shared/models';
import { StudentNoContact } from '../../shared/models/report.model';
import { ReportViewerComponent } from '../../shared/components/report-viewer/report-viewer.component';
import { DataTableColumn } from '../../shared/components/data-table/data-table-column';
import { AdvancedFilterConfig, AdvancedFilterModel } from '../../shared/components/advanced-filter/advanced-filter-config';

@Component({
  selector: 'app-students-no-contact-report-page',
  standalone: true,
  imports: [RouterLink, ReportViewerComponent],
  templateUrl: './students-no-contact-report.page.html',
  styleUrls: ['./servant-follow-up.page.css'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class StudentsNoContactReportPage implements OnInit {
  private reportsService = inject(ReportsService);
  private usersService = inject(UsersService);

  servants = signal<User[]>([]);
  data = signal<StudentNoContact[]>([]);
  loading = signal(false);
  error = signal('');
  page = signal(1);
  pageSize = signal(10);
  totalCount = signal(0);
  pageSizeOptions = [5, 10, 20, 50];
  sortBy = signal<string | null>('fullName');
  sortDesc = signal(false);
  filterModel = signal<AdvancedFilterModel>({ days: 14 });

  filterConfig = computed<AdvancedFilterConfig>(() => {
    const list = this.servants();
    return [
      {
        key: 'days',
        label: 'خلال (يوم)',
        type: 'select',
        options: [
          { value: '7', label: '7' },
          { value: '14', label: '14' },
          { value: '30', label: '30' },
        ],
      },
      {
        key: 'servantId',
        label: 'الخادم',
        type: 'select',
        placeholder: 'الكل',
        options: list.map((s) => ({ value: s.id ?? '', label: s.fullName ?? s.userName ?? '' })),
      },
    ];
  });

  readonly columns: DataTableColumn<StudentNoContact>[] = [
    { key: 'fullName', header: 'المخدوم', sortable: true },
    { key: 'servantName', header: 'الخادم', sortable: true },
    { key: 'lastContactDate', header: 'آخر تواصل', sortable: true, format: 'date' },
  ];

  actionsTplRef = viewChild<TemplateRef<{ $implicit: StudentNoContact }>>('rowActionsTpl');

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
    const daysVal = model['days'];
    const servantIdVal = model['servantId'];
    const days = daysVal != null && String(daysVal).trim() !== '' ? parseInt(String(daysVal), 10) : 14;
    const params: {
      days?: number;
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
      days: isNaN(days) ? 14 : days,
    };
    if (servantIdVal != null && String(servantIdVal).trim()) params.servantId = String(servantIdVal).trim();

    this.reportsService.getStudentsWithNoRecentContact(params).subscribe({
      next: (res) => {
        this.data.set(res.items ?? []);
        this.totalCount.set(res.totalCount ?? 0);
        this.page.set(res.page ?? this.page());
        this.loading.set(false);
      },
      error: () => {
        this.error.set('فشل تحميل القائمة');
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

  onRowClick(row: StudentNoContact): void {
    // Optional: navigate to student detail
  }
}
