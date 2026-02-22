import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { ReportsService, StudentNoContact } from '../../services/reports.service';
import { UsersService, User } from '../../services/users.service';
import { ContentHeaderComponent, LoaderComponent, CardComponent, PaginationComponent } from '../../components/common/common';

@Component({
  selector: 'app-students-no-contact-report-page',
  standalone: true,
  imports: [CommonModule, RouterLink, FormsModule, ContentHeaderComponent, LoaderComponent, CardComponent, PaginationComponent],
  templateUrl: './students-no-contact-report.page.html',
  styleUrls: ['./servant-follow-up.page.css']
})
export class StudentsNoContactReportPage implements OnInit {
  servants: User[] = [];
  list: StudentNoContact[] = [];
  loading = false;
  error = '';
  daysNoContact = 14;
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
    const params: { days?: number; servantId?: string; page: number; pageSize: number } = { days: this.daysNoContact, page: this.page, pageSize: this.pageSize };
    if (this.servantIdFilter) params.servantId = this.servantIdFilter;
    this.reportsService.getStudentsWithNoRecentContact(params).subscribe({
      next: (data) => {
        this.list = data.items ?? [];
        this.totalCount = data.totalCount ?? 0;
        this.page = data.page ?? 1;
        this.loading = false;
      },
      error: () => { this.error = 'فشل تحميل القائمة'; this.loading = false; }
    });
  }

  onApplyFilters() { this.page = 1; this.loadData(); }
  goToPage(p: number) { this.page = p; this.loadData(); }
  setPageSize(n: number) { this.pageSize = Number(n); this.page = 1; this.loadData(); }
}
