import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute } from '@angular/router';
import { StudentQueriesService, StudentEditLogDto } from '../../services/student-queries.service';
import { FollowUpService, CallLogDto, HomeVisitDto } from '../../services/follow-up.service';
import { ContentHeaderComponent, LoaderComponent, DetailSectionComponent } from '../../components/common/common';

@Component({
  selector: 'app-student-detail-page',
  standalone: true,
  imports: [CommonModule, ContentHeaderComponent, LoaderComponent, DetailSectionComponent],
  templateUrl: './student-detail.page.html',
  styleUrls: ['./student-detail.page.css']
})
export class StudentDetailPage implements OnInit {
  student: any = null;
  calls: CallLogDto[] = [];
  visits: HomeVisitDto[] = [];
  editHistory: StudentEditLogDto[] = [];
  loading = true;

  constructor(
    private route: ActivatedRoute,
    private studentQueries: StudentQueriesService,
    private followUp: FollowUpService
  ) {}

  ngOnInit() {
    const id = this.route.snapshot.paramMap.get('id');
    if (!id) return;
    this.loading = true;
    this.studentQueries.getById(id).subscribe({
      next: (s) => {
        this.student = s;
        this.followUp.getCallHistory(id).subscribe({ next: (c) => this.calls = c || [] });
        this.followUp.getVisitHistory(id).subscribe({ next: (v) => this.visits = v || [] });
        this.studentQueries.getEditHistory(id).subscribe({ next: (h) => this.editHistory = h || [] });
        this.loading = false;
      },
      error: () => this.loading = false
    });
  }

  formatDate(d: string | null): string {
    if (!d) return '—';
    const date = new Date(d);
    return date.toLocaleDateString('ar-EG', { day: '2-digit', month: '2-digit', year: 'numeric' });
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
