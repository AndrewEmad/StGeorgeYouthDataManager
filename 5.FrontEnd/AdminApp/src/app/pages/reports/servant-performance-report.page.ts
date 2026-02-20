import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { ReportsService, PagedReport, ServantPerformance } from '../../services/reports.service';

@Component({
  selector: 'app-servant-performance-report-page',
  standalone: true,
  imports: [CommonModule, RouterLink, FormsModule],
  templateUrl: './servant-performance-report.page.html',
  styleUrls: ['./servant-follow-up.page.css']
})
export class ServantPerformanceReportPage implements OnInit {
  perfResult: PagedReport<ServantPerformance> | null = null;
  loading = false;
  page = 1;
  pageSize = 10;
  pageSizeOptions = [5, 10, 20, 50];

  constructor(
    private reportsService: ReportsService,
    private router: Router
  ) {}

  get totalPages(): number {
    if (!this.perfResult) return 1;
    return Math.max(1, Math.ceil(this.perfResult.totalCount / this.pageSize));
  }

  viewServant(servantId: string) {
    this.router.navigate(['/dashboard/servants', servantId]);
  }

  loadData() {
    this.loading = true;
    this.reportsService.getServantPerformancesPaged({ page: this.page, pageSize: this.pageSize }).subscribe({
      next: (data) => {
        this.perfResult = data;
        this.page = data.page ?? this.page;
        this.loading = false;
      },
      error: () => { this.loading = false; }
    });
  }

  goToPage(p: number) {
    this.page = p;
    this.loadData();
  }

  setPageSize(n: number) {
    this.pageSize = Number(n);
    this.page = 1;
    this.loadData();
  }

  ngOnInit() {
    this.loadData();
  }
}
