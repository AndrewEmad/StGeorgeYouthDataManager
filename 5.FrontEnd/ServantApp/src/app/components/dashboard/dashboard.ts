import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReportsService } from '../../services/reports.service';
import { AuthService } from '../../services/auth.service';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './dashboard.html',
  styleUrls: ['./dashboard.css']
})
export class DashboardComponent implements OnInit {
  stats: any = null;
  loading = true;
  servantName = '';

  constructor(private reportsService: ReportsService, private authService: AuthService) {
    this.servantName = this.authService.currentUser()?.fullName || '';
  }

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
        // Fallback or mock if API fails
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

  logout() {
    this.authService.logout();
  }
}
