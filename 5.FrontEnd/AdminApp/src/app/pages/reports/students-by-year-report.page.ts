import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { ReportsService, StudentsByGroup } from '../../services/reports.service';

@Component({
  selector: 'app-students-by-year-report-page',
  standalone: true,
  imports: [CommonModule, RouterLink, FormsModule],
  templateUrl: './students-by-year-report.page.html',
  styleUrls: ['./servant-follow-up.page.css']
})
export class StudentsByYearReportPage implements OnInit {
  list: StudentsByGroup[] = [];
  loading = false;
  error = '';
  page = 1;
  pageSize = 10;
  totalCount = 0;
  pageSizeOptions = [5, 10, 20, 50];

  constructor(private reportsService: ReportsService) {}

  get totalPages(): number { return Math.max(1, Math.ceil(this.totalCount / this.pageSize)); }

  ngOnInit() { this.loadData(); }

  loadData() {
    this.loading = true;
    this.error = '';
    this.reportsService.getStudentsByAcademicYear({ page: this.page, pageSize: this.pageSize }).subscribe({
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
