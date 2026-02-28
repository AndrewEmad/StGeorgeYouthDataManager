import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { ReportsService } from '../../services/reports.service';
import { AuthService } from '../../services/auth.service';
import { StudentQueriesService } from '../../services/student-queries.service';
import { LoaderComponent, CardComponent } from '../../components/common/common';

@Component({
  selector: 'app-dashboard-page',
  standalone: true,
  imports: [CommonModule, RouterLink, FormsModule, LoaderComponent, CardComponent],
  templateUrl: './dashboard.page.html',
  styleUrls: ['./dashboard.page.css']
})
export class DashboardPage implements OnInit {
  stats: any = null;
  loading = true;
  showBulkWhatsAppModal = false;
  bulkWhatsAppStep: 1 | 2 = 1;
  bulkWhatsAppMessage = '';
  assignedForBulk: { id: string; fullName: string; phone: string }[] = [];
  loadingAssignedForBulk = false;

  constructor(
    private reportsService: ReportsService,
    public authService: AuthService,
    private studentQueriesService: StudentQueriesService
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

  openBulkWhatsAppModal() {
    this.bulkWhatsAppMessage = '';
    this.bulkWhatsAppStep = 1;
    this.assignedForBulk = [];
    this.showBulkWhatsAppModal = true;
  }

  closeBulkWhatsAppModal() {
    this.showBulkWhatsAppModal = false;
  }

  nextBulkStep() {
    const userId = this.authService.currentUser()?.userId;
    if (!userId) return;
    this.loadingAssignedForBulk = true;
    this.studentQueriesService.getByServantId(userId).subscribe({
      next: (list) => {
        this.assignedForBulk = (list || []).filter((s: any) => s?.phone).map((s: any) => ({ id: s.id, fullName: s.fullName || '—', phone: s.phone }));
        this.loadingAssignedForBulk = false;
        this.bulkWhatsAppStep = 2;
      },
      error: () => { this.loadingAssignedForBulk = false; }
    });
  }

  prevBulkStep() {
    this.bulkWhatsAppStep = 1;
  }

  getWaMeUrl(phone: string, text: string): string {
    const digits = (phone || '').replace(/\D/g, '');
    if (!digits) return '#';
    const q = text?.trim() ? `?text=${encodeURIComponent(text.trim())}` : '';
    return `https://wa.me/${digits}${q}`;
  }
}
