import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterLink } from '@angular/router';
import { ReportsService } from '../../services/reports.service';
import { UsersService, User } from '../../services/users.service';

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
  priest: User | null = null;

  constructor(
    private reportsService: ReportsService,
    private usersService: UsersService,
    private router: Router
  ) {}

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
    this.usersService.getPaged({ page: 1, pageSize: 1, role: 'Priest' }).subscribe({
      next: (res) => {
        this.priest = res.items?.length ? res.items[0] : null;
      }
    });
  }
}
