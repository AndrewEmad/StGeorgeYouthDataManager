import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { ReportsService, ServantActivitySummary } from '../../services/reports.service';
import { UsersService, User } from '../../services/users.service';

@Component({
  selector: 'app-servant-activity-report-page',
  standalone: true,
  imports: [CommonModule, RouterLink, FormsModule],
  templateUrl: './servant-activity-report.page.html',
  styleUrls: ['./servant-follow-up.page.css']
})
export class ServantActivityReportPage implements OnInit {
  servants: User[] = [];
  activitySummary: ServantActivitySummary[] = [];
  loading = false;
  error = '';
  dateFrom = '';
  dateTo = '';
  servantIdFilter = '';
  page = 1;
  pageSize = 10;
  totalCount = 0;
  pageSizeOptions = [5, 10, 20, 50];

  constructor(
    private reportsService: ReportsService,
    private usersService: UsersService
  ) {}

  get totalPages(): number { return Math.max(1, Math.ceil(this.totalCount / this.pageSize)); }

  ngOnInit() {
    this.usersService.getPaged({ page: 1, pageSize: 500 }).subscribe({ next: (res) => { this.servants = res.items ?? []; } });
    this.loadData();
  }

  loadData() {
    this.loading = true;
    this.error = '';
    const params: { dateFrom?: string; dateTo?: string; servantId?: string; page: number; pageSize: number } = { page: this.page, pageSize: this.pageSize };
    if (this.dateFrom) params.dateFrom = this.dateFrom;
    if (this.dateTo) params.dateTo = this.dateTo;
    if (this.servantIdFilter) params.servantId = this.servantIdFilter;
    this.reportsService.getServantActivitySummary(params).subscribe({
      next: (data) => {
        this.activitySummary = data.items ?? [];
        this.totalCount = data.totalCount ?? 0;
        this.page = data.page ?? 1;
        this.loading = false;
      },
      error: () => { this.error = 'فشل تحميل التقرير'; this.loading = false; }
    });
  }

  onApplyFilters() { this.page = 1; this.loadData(); }
  goToPage(p: number) { this.page = p; this.loadData(); }
  setPageSize(n: number) { this.pageSize = Number(n); this.page = 1; this.loadData(); }
}
