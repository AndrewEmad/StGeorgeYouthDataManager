import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { UsersService } from '../../services/users.service';
import { ReportsService } from '../../services/reports.service';
import { FollowUpService, CallLogDto, HomeVisitDto } from '../../services/follow-up.service';

@Component({
  selector: 'app-servant-detail-page',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './servant-detail.page.html',
  styleUrls: ['./servant-detail.page.css']
})
export class ServantDetailPage implements OnInit {
  servant: any = null;
  performance: any = null;
  calls: CallLogDto[] = [];
  visits: HomeVisitDto[] = [];
  loading = true;

  constructor(
    private route: ActivatedRoute,
    private usersService: UsersService,
    private reportsService: ReportsService,
    private followUp: FollowUpService
  ) {}

  ngOnInit() {
    const id = this.route.snapshot.paramMap.get('id');
    if (!id) return;
    this.loading = true;
    this.usersService.getById(id).subscribe({
      next: (user) => {
        this.servant = user;
        let done = 0;
        const check = () => { if (++done === 3) this.loading = false; };
        this.reportsService.getServantPerformance(id).subscribe({ next: (p) => { this.performance = p; check(); }, error: check });
        this.followUp.getServantCallHistory(id).subscribe({ next: (c) => { this.calls = c || []; check(); }, error: check });
        this.followUp.getServantVisitHistory(id).subscribe({ next: (v) => { this.visits = v || []; check(); }, error: check });
      },
      error: () => this.loading = false
    });
  }

  formatDate(d: string | null): string {
    if (!d) return '—';
    return new Date(d).toLocaleDateString('ar-EG', { day: '2-digit', month: '2-digit', year: 'numeric' });
  }

  callStatusLabel(n: number): string {
    const map: Record<number, string> = { 0: 'لم يرد', 1: 'رد', 2: 'مشغول', 3: 'مغلق', 4: 'رقم خاطئ' };
    return map[n] ?? String(n);
  }

  visitOutcomeLabel(n: number): string {
    const map: Record<number, string> = { 0: 'تمت الزيارة', 1: 'غير موجود', 2: 'رفض الاستقبال', 3: 'مؤجلة' };
    return map[n] ?? String(n);
  }
}
