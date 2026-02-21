import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReportsService } from '../../services/reports.service';
import { RouterLink } from '@angular/router';
import { ServantHeaderComponent } from '../servant-header/servant-header';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, RouterLink, ServantHeaderComponent],
  templateUrl: './dashboard.html',
  styleUrls: ['./dashboard.css']
})
export class DashboardComponent implements OnInit {
  stats: any = null;
  loading = true;

  constructor(private reportsService: ReportsService) {}

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
