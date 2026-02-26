import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { ReportsService, StudentsByGroup } from '../../services/reports.service';
import { ContentHeaderComponent, LoaderComponent, CardComponent, PaginationComponent } from '../../components/common/common';

const MONTH_NAMES = ['يناير', 'فبراير', 'مارس', 'أبريل', 'مايو', 'يونيو', 'يوليو', 'أغسطس', 'سبتمبر', 'أكتوبر', 'نوفمبر', 'ديسمبر'];

@Component({
  selector: 'app-students-by-birth-month-report-page',
  standalone: true,
  imports: [CommonModule, RouterLink, FormsModule, ContentHeaderComponent, LoaderComponent, CardComponent, PaginationComponent],
  templateUrl: './students-by-birth-month-report.page.html',
  styleUrls: ['./servant-follow-up.page.css']
})
export class StudentsByBirthMonthReportPage implements OnInit {
  list: StudentsByGroup[] = [];
  loading = false;
  error = '';
  page = 1;
  pageSize = 10;
  totalCount = 0;
  pageSizeOptions = [5, 10, 20, 50];

  constructor(private reportsService: ReportsService) {}

  get totalPages(): number { return Math.max(1, Math.ceil(this.totalCount / this.pageSize)); }

  monthLabel(groupKey: string): string {
    if (!groupKey || groupKey === '0') return 'غير محدد';
    const n = +groupKey;
    if (n >= 1 && n <= 12) return MONTH_NAMES[n - 1];
    return groupKey;
  }

  canViewStudents(groupKey: string): boolean {
    const n = +groupKey;
    return n >= 1 && n <= 12;
  }

  ngOnInit() { this.loadData(); }

  loadData() {
    this.loading = true;
    this.error = '';
    this.reportsService.getStudentsByBirthMonth({ page: this.page, pageSize: this.pageSize }).subscribe({
      next: (data) => {
        this.list = data.items ?? [];
        this.totalCount = data.totalCount ?? 0;
        this.page = data.page ?? 1;
        this.loading = false;
      },
      error: () => { this.error = 'فشل تحميل التقرير'; this.loading = false; }
    });
  }

  goToPage(p: number) { this.page = p; this.loadData(); }
  setPageSize(n: number) { this.pageSize = Number(n); this.page = 1; this.loadData(); }
}
