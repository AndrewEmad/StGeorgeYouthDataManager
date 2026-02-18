import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterLink } from '@angular/router';
import { ReportsService } from '../../services/reports.service';

@Component({
  selector: 'app-stats',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './stats.html',
  styleUrls: ['./stats.css']
})
export class StatsComponent implements OnInit {
  stats: any = null;
  loading = true;

  constructor(private reportsService: ReportsService, private router: Router) {}

  viewServant(servantId: string) {
    this.router.navigate(['/dashboard/servants', servantId]);
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
  }
}
