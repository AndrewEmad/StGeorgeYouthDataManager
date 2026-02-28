import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { ReportsService } from '../../services/reports.service';
import { AuthService } from '../../services/auth.service';
import { StudentQueriesService } from '../../services/student-queries.service';
import { LoaderComponent, CardComponent } from '../../components/common/common';
import { BulkWhatsAppModalComponent } from '../../components/dashboard/bulk-whatsapp-modal/bulk-whatsapp-modal';
import { BulkSmsModalComponent } from '../../components/dashboard/bulk-sms-modal/bulk-sms-modal';

@Component({
  selector: 'app-dashboard-page',
  standalone: true,
  imports: [
    CommonModule, RouterLink, FormsModule,
    LoaderComponent, CardComponent,
    BulkWhatsAppModalComponent, BulkSmsModalComponent
  ],
  templateUrl: './dashboard.page.html',
  styleUrls: ['./dashboard.page.css']
})
export class DashboardPage implements OnInit {
  stats: any = null;
  loading = true;

  showBulkWhatsAppModal = false;
  showBulkSmsModal = false;

  constructor(
    private reportsService: ReportsService,
    public authService: AuthService
  ) {}

  ngOnInit() {
    this.loadStats();
  }

  get currentUserId() {
    return this.authService.currentUser()?.userId;
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

  openBulkWhatsAppModal() {
    this.showBulkWhatsAppModal = true;
  }

  closeBulkWhatsAppModal() {
    this.showBulkWhatsAppModal = false;
  }

  openBulkSmsModal() {
    this.showBulkSmsModal = true;
  }

  closeBulkSmsModal() {
    this.showBulkSmsModal = false;
  }
}

