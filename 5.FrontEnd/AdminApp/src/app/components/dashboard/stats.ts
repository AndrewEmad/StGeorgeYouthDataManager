import { Component, OnInit, signal, inject, ChangeDetectionStrategy } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { ReportsService } from '../../services/reports.service';
import { UsersService } from '../../services/users.service';
import { User } from '../../shared/models';
import { ContentHeaderComponent, LoaderComponent, StatItemComponent, CardComponent } from '../common/common';
import { DateFormatPipe } from '../../shared/pipes/date-format.pipe';

@Component({
  selector: 'app-stats',
  standalone: true,
  imports: [RouterLink, ContentHeaderComponent, LoaderComponent, StatItemComponent, CardComponent, DateFormatPipe],
  templateUrl: './stats.html',
  styleUrls: ['./stats.css'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class StatsComponent implements OnInit {
  private reportsService = inject(ReportsService);
  private usersService = inject(UsersService);
  private router = inject(Router);

  stats = signal<Record<string, unknown> | null>(null);
  loading = signal(true);
  priest = signal<User | null>(null);

  statValue(key: string): number {
    const v = this.stats()?.[key];
    return typeof v === 'number' ? v : Number(v) || 0;
  }

  recentActivities(): { description: string; date: string }[] {
    const s = this.stats();
    if (!s || !Array.isArray(s['recentActivities'])) return [];
    return s['recentActivities'] as { description: string; date: string }[];
  }

  viewServant(servantId: string): void {
    this.router.navigate(['/dashboard/servants', servantId]);
  }

  ngOnInit(): void {
    this.reportsService.getManagerDashboard().subscribe({
      next: (data: Record<string, unknown>) => {
        this.stats.set(data);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
      },
    });
    this.usersService.getPaged({ page: 1, pageSize: 1, role: 'Priest' }).subscribe({
      next: (res) => {
        this.priest.set(res.items?.length ? res.items[0] ?? null : null);
      },
    });
  }
}
