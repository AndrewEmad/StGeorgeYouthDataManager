import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { ReportsService } from '../../services/reports.service';
import { UsersService, User } from '../../services/users.service';
import { PagedReport, ServantPerformance } from '../../services/reports.service';

@Component({
  selector: 'app-stats',
  standalone: true,
  imports: [CommonModule, RouterLink, FormsModule],
  templateUrl: './stats.html',
  styleUrls: ['./stats.css']
})
export class StatsComponent implements OnInit {
  stats: any = null;
  loading = true;
  priest: User | null = null;
  perfPage = 1;
  perfPageSize = 10;
  perfPageSizeOptions = [5, 10, 20, 50];
  perfResult: PagedReport<ServantPerformance> | null = null;
  perfLoading = false;

  constructor(
    private reportsService: ReportsService,
    private usersService: UsersService,
    private router: Router
  ) {}

  viewServant(servantId: string) {
    this.router.navigate(['/dashboard/servants', servantId]);
  }

  get servantPerformancesTotalPages(): number {
    if (!this.perfResult) return 1;
    return Math.max(1, Math.ceil(this.perfResult.totalCount / this.perfPageSize));
  }

  loadPerfPage() {
    this.perfLoading = true;
    this.reportsService.getServantPerformancesPaged({ page: this.perfPage, pageSize: this.perfPageSize }).subscribe({
      next: (data) => {
        this.perfResult = data;
        this.perfPage = data.page ?? this.perfPage;
        this.perfLoading = false;
      },
      error: () => { this.perfLoading = false; }
    });
  }

  goToPerfPage(p: number) {
    this.perfPage = p;
    this.loadPerfPage();
  }

  setPerfPageSize(n: number) {
    this.perfPageSize = Number(n);
    this.perfPage = 1;
    this.loadPerfPage();
  }

  ngOnInit() {
    this.reportsService.getManagerDashboard().subscribe({
      next: (data: any) => {
        this.stats = data;
        this.loading = false;
      },
      error: () => {
        this.loading = false;
      }
    });
    this.usersService.getPaged({ page: 1, pageSize: 1, role: 'Priest' }).subscribe({
      next: (res) => {
        this.priest = res.items?.length ? res.items[0] : null;
      }
    });
    this.loadPerfPage();
  }
}
