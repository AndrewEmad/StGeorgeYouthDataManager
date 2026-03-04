import { Component, OnInit, signal, inject, ChangeDetectionStrategy, TemplateRef, viewChild } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { ReportsService } from '../../services/reports.service';
import { ServantPerformance } from '../../shared/models/report.model';
import { ReportViewerComponent } from '../../shared/components/report-viewer/report-viewer.component';
import { DataTableColumn } from '../../shared/components/data-table/data-table-column';

@Component({
  selector: 'app-servant-performance-report-page',
  standalone: true,
  imports: [RouterLink, ReportViewerComponent],
  templateUrl: './servant-performance-report.page.html',
  styleUrls: ['./servant-follow-up.page.css'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ServantPerformanceReportPage implements OnInit {
  private reportsService = inject(ReportsService);
  private router = inject(Router);

  actionsTplRef = viewChild<TemplateRef<{ $implicit: ServantPerformance }>>('rowActionsTpl');

  readonly columns: DataTableColumn<ServantPerformance>[] = [
    { key: 'fullName', header: 'الخادم', sortable: true },
    { key: 'assignedStudentsCount', header: 'عدد المخدومين المخصصين', sortable: true },
    { key: 'callsThisWeek', header: 'مكالمات هذا الأسبوع', sortable: true },
    { key: 'visitsThisWeek', header: 'زيارات هذا الأسبوع', sortable: true },
  ];

  data = signal<ServantPerformance[]>([]);
  loading = signal(false);
  totalCount = signal(0);
  page = signal(1);
  pageSize = signal(10);
  pageSizeOptions = [5, 10, 20, 50];
  sortBy = signal<string | null>('fullName');
  sortDesc = signal(false);

  ngOnInit(): void {
    this.loadData();
  }

  loadData(): void {
    this.loading.set(true);
    this.reportsService
      .getServantPerformancesPaged({
        page: this.page(),
        pageSize: this.pageSize(),
        sortBy: this.sortBy() ?? undefined,
        sortDesc: this.sortDesc(),
      })
      .subscribe({
        next: (res) => {
          this.data.set(res.items ?? []);
          this.totalCount.set(res.totalCount ?? 0);
          this.page.set(res.page ?? this.page());
          this.loading.set(false);
        },
        error: () => this.loading.set(false),
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

  onRowClick(row: ServantPerformance): void {
    this.router.navigate(['/dashboard/servants', row.servantId]);
  }
}
