import { ChangeDetectionStrategy, Component, computed, DestroyRef, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ReportsService } from '../../services/reports.service';
import { AuthService } from '../../core/services/auth.service';
import { LoaderComponent, CardComponent } from '../../shared/components';
import { BulkWhatsAppModalComponent } from '../../components/dashboard/bulk-whatsapp-modal/bulk-whatsapp-modal';
import { BulkSmsModalComponent } from '../../components/dashboard/bulk-sms-modal/bulk-sms-modal';
import type { ServantDashboardStats } from '../../shared/models/reports.model';

@Component({
  selector: 'app-dashboard-page',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    RouterLink,
    LoaderComponent,
    CardComponent,
    BulkWhatsAppModalComponent,
    BulkSmsModalComponent,
  ],
  templateUrl: './dashboard.page.html',
  styleUrls: ['./dashboard.page.css'],
})
export class DashboardPage {
  private readonly reportsService = inject(ReportsService);
  readonly authService = inject(AuthService);
  private readonly destroyRef = inject(DestroyRef);

  readonly stats = signal<ServantDashboardStats | null>(null);
  readonly loading = signal(true);
  readonly showBulkWhatsAppModal = signal(false);
  readonly showBulkSmsModal = signal(false);

  readonly currentUserId = computed(() => this.authService.currentUser()?.userId ?? null);

  constructor() {
    this.loadStats();
  }

  loadStats(): void {
    this.reportsService
      .getServantDashboard()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (data: ServantDashboardStats) => {
          this.stats.set(data);
          this.loading.set(false);
        },
        error: () => {
          this.stats.set({
            totalStudents: 0,
            callsThisWeek: 0,
            visitsThisWeek: 0,
            studentsNeedingFollowUp: 0,
            studentsNotContacted: 0,
          });
          this.loading.set(false);
        },
      });
  }

  openBulkWhatsAppModal(): void {
    this.showBulkWhatsAppModal.set(true);
  }

  closeBulkWhatsAppModal(): void {
    this.showBulkWhatsAppModal.set(false);
  }

  openBulkSmsModal(): void {
    this.showBulkSmsModal.set(true);
  }

  closeBulkSmsModal(): void {
    this.showBulkSmsModal.set(false);
  }
}
