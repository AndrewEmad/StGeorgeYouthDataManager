import { Component, OnInit, signal, inject, ChangeDetectionStrategy, viewChild, TemplateRef } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ReportsService } from '../../services/reports.service';
import { StudentsByGroup } from '../../shared/models/report.model';
import { ReportViewerComponent } from '../../shared/components/report-viewer/report-viewer.component';
import { DataTableColumn } from '../../shared/components/data-table/data-table-column';

@Component({
  selector: 'app-students-by-year-report-page',
  standalone: true,
  imports: [RouterLink, ReportViewerComponent],
  templateUrl: './students-by-year-report.page.html',
  styleUrls: ['./servant-follow-up.page.css'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class StudentsByYearReportPage implements OnInit {
  private reportsService = inject(ReportsService);

  data = signal<StudentsByGroup[]>([]);
  loading = signal(false);
  error = signal('');
  page = signal(1);
  pageSize = signal(10);
  totalCount = signal(0);
  pageSizeOptions = [5, 10, 20, 50];
  sortBy = signal<string | null>('groupKey');
  sortDesc = signal(false);

  actionsTplRef = viewChild<TemplateRef<{ $implicit: StudentsByGroup }>>('rowActionsTpl');

  readonly columns: DataTableColumn<StudentsByGroup>[] = [
    { key: 'groupKey', header: 'السنة الدراسية', sortable: true },
    { key: 'count', header: 'العدد', sortable: true },
  ];

  ngOnInit(): void {
    this.loadData();
  }

  loadData(): void {
    this.loading.set(true);
    this.error.set('');
    this.reportsService
      .getStudentsByAcademicYear({
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
