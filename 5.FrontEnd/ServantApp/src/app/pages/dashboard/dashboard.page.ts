import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { ReportsService } from '../../services/reports.service';
import { AuthService } from '../../services/auth.service';
import { LoaderComponent, CardComponent } from '../../components/common/common';

@Component({
  selector: 'app-dashboard-page',
  standalone: true,
  imports: [CommonModule, RouterLink, LoaderComponent, CardComponent],
  templateUrl: './dashboard.page.html',
  styleUrls: ['./dashboard.page.css']
})
export class DashboardPage implements OnInit {
  stats: any = null;
  loading = true;

  constructor(
    private reportsService: ReportsService,
    public authService: AuthService
  ) {}

  ngOnInit() {
    this.loadStats();
  }

  loadStats() {
    this.reportsService.getServantDashboard().subscribe({
      next: (data: any) => {
        this.stats = data;
        this.loading = false;
      },
      error: () => {
        this.stats = {
          totalStudents: 0,
          callsThisWeek: 0,
          visitsThisWeek: 0,
          studentsNeedingFollowUp: 0,
          studentsNotContacted: 0
        };
        this.loading = false;
      }
    });
  }
}
