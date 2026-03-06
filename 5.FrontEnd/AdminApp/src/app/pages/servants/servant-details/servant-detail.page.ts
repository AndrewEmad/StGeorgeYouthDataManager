import { Component, OnDestroy, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import {
  ContentHeaderComponent,
  DetailSectionComponent,
  LoaderComponent,
  ModalComponent,
  StatItemComponent,
} from '../../../components/common/common';
import { FollowUpService } from '../../../services/follow-up.service';
import { ReportsService } from '../../../services/reports.service';
import { UsersService } from '../../../services/users.service';
import { CallLogDto, HomeVisitDto, HomeVisitParticipantDto } from '../../../shared/models';
import { CallStatusPipe } from '../../../shared/pipes/call-status.pipe';
import { DateFormatPipe } from '../../../shared/pipes/date-format.pipe';
import { VisitOutcomePipe } from '../../../shared/pipes/visit-outcome.pipe';

@Component({
  selector: 'app-servant-detail-page',
  standalone: true,
  imports: [
    FormsModule,
    RouterLink,
    ContentHeaderComponent,
    LoaderComponent,
    DetailSectionComponent,
    StatItemComponent,
    ModalComponent,
    DateFormatPipe,
    CallStatusPipe,
    VisitOutcomePipe,
  ],
  templateUrl: './servant-detail.page.html',
  styleUrls: ['./servant-detail.page.css'],
})
export class ServantDetailPage implements OnInit, OnDestroy {
  servant: any = null;
  servantPhotoUrl: string | null = null;
  performance: any = null;
  calls: CallLogDto[] = [];
  visits: HomeVisitDto[] = [];
  loading = true;
  showPasswordModal = false;
  newPassword = '';
  confirmPassword = '';
  passwordError = '';
  passwordLoading = false;

  constructor(
    private route: ActivatedRoute,
    private usersService: UsersService,
    private reportsService: ReportsService,
    private followUp: FollowUpService,
  ) {}

  ngOnInit() {
    const id = this.route.snapshot.paramMap.get('id');
    if (!id) return;
    this.loading = true;
    this.usersService.getById(id).subscribe({
      next: (user) => {
        this.servant = user;
        if (user.photoPath) {
          this.usersService.getPhotoBlobUrl(id).subscribe((url) => (this.servantPhotoUrl = url));
        }
        let done = 0;
        const check = () => {
          if (++done === 3) this.loading = false;
        };
        this.reportsService.getServantPerformance(id).subscribe({
          next: (p) => {
            this.performance = p;
            check();
          },
          error: check,
        });
        this.followUp.getServantCallHistory(id).subscribe({
          next: (c) => {
            this.calls = c || [];
            check();
          },
          error: check,
        });
        this.followUp.getServantVisitHistory(id).subscribe({
          next: (v) => {
            this.visits = v || [];
            check();
          },
          error: check,
        });
      },
      error: () => (this.loading = false),
    });
  }

  ngOnDestroy() {
    if (this.servantPhotoUrl) URL.revokeObjectURL(this.servantPhotoUrl);
  }

  formatDate(d: string | null): string {
    if (!d) return '—';
    return new Date(d).toLocaleDateString('ar-EG', {
      day: '2-digit',
      month: '2-digit',
      year: 'numeric',
    });
  }

  callStatusLabel(n: number): string {
    const map: Record<number, string> = {
      0: 'لم يرد',
      1: 'رد',
      2: 'مشغول',
      3: 'مغلق',
      4: 'رقم خاطئ',
    };
    return map[n] ?? String(n);
  }

  visitOutcomeLabel(n: number): string {
    const map: Record<number, string> = {
      0: 'تمت الزيارة',
      1: 'غير موجود',
      2: 'رفض الاستقبال',
      3: 'مؤجلة',
    };
    return map[n] ?? String(n);
  }

  formatParticipants(participants: HomeVisitParticipantDto[] | undefined): string {
    if (!participants?.length) return '—';
    return (
      participants
        .map((p) => p.servantName)
        .filter(Boolean)
        .join('، ') || '—'
    );
  }

  openChangePassword() {
    this.showPasswordModal = true;
    this.newPassword = '';
    this.confirmPassword = '';
    this.passwordError = '';
  }

  closeChangePassword() {
    this.showPasswordModal = false;
    this.passwordError = '';
  }

  submitChangePassword() {
    this.passwordError = '';
    if (!this.newPassword.trim()) {
      this.passwordError = 'أدخل كلمة المرور الجديدة';
      return;
    }
    if (this.newPassword.length < 6) {
      this.passwordError = 'كلمة المرور يجب أن تكون 6 أحرف على الأقل';
      return;
    }
    if (this.newPassword !== this.confirmPassword) {
      this.passwordError = 'كلمة المرور وتأكيدها غير متطابقين';
      return;
    }
    this.passwordLoading = true;
    this.usersService.setPassword(this.servant.id, this.newPassword).subscribe({
      next: () => {
        this.passwordLoading = false;
        this.closeChangePassword();
      },
      error: (err) => {
        this.passwordError = err.error?.message || err.error || 'فشل تعيين كلمة المرور';
        this.passwordLoading = false;
      },
    });
  }
}
