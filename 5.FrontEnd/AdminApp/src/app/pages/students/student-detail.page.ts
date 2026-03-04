import {
  Component,
  inject,
  signal,
  ChangeDetectionStrategy,
  DestroyRef,
  OnInit,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute } from '@angular/router';
import { StudentQueriesService } from '../../services/student-queries.service';
import { FollowUpService } from '../../services/follow-up.service';
import {
  StudentEditLogDto,
  CallLogDto,
  HomeVisitDto,
  HomeVisitParticipantDto,
} from '../../shared/models';
import { ContentHeaderComponent, LoaderComponent, DetailSectionComponent } from '../../components/common/common';
import { DateFormatPipe } from '../../shared/pipes/date-format.pipe';
import { CallStatusPipe } from '../../shared/pipes/call-status.pipe';
import { VisitOutcomePipe } from '../../shared/pipes/visit-outcome.pipe';

@Component({
  selector: 'app-student-detail-page',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    ContentHeaderComponent,
    LoaderComponent,
    DetailSectionComponent,
    DateFormatPipe,
    CallStatusPipe,
    VisitOutcomePipe,
  ],
  templateUrl: './student-detail.page.html',
  styleUrls: ['./student-detail.page.css'],
})
export class StudentDetailPage implements OnInit {
  private route = inject(ActivatedRoute);
  private destroyRef = inject(DestroyRef);
  private studentQueries = inject(StudentQueriesService);
  private followUp = inject(FollowUpService);

  student = signal<{
    fullName?: string;
    phone?: string;
    birthDate?: string | null;
    area?: string | null;
    college?: string | null;
    academicYear?: string | null;
    confessionFather?: string | null;
    servantName?: string | null;
    createdAt?: string | null;
    updatedAt?: string | null;
  } | null>(null);
  studentPhotoUrl = signal<string | null>(null);
  calls = signal<CallLogDto[]>([]);
  visits = signal<HomeVisitDto[]>([]);
  editHistory = signal<StudentEditLogDto[]>([]);
  loading = signal(true);

  constructor() {
    this.destroyRef.onDestroy(() => {
      const url = this.studentPhotoUrl();
      if (url) URL.revokeObjectURL(url);
    });
  }

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (!id) return;
    this.loading.set(true);
    this.studentQueries
      .getById(id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (s) => {
          this.student.set(s);
          if (s.photoPath) {
            this.studentQueries
              .getPhotoBlobUrl(id)
              .pipe(takeUntilDestroyed(this.destroyRef))
              .subscribe((url) => this.studentPhotoUrl.set(url));
          }
          this.followUp
            .getCallHistory(id)
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe({ next: (c) => this.calls.set(c || []) });
          this.followUp
            .getVisitHistory(id)
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe({ next: (v) => this.visits.set(v || []) });
          this.studentQueries
            .getEditHistory(id)
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe({ next: (h) => this.editHistory.set(h || []) });
          this.loading.set(false);
        },
        error: () => this.loading.set(false),
      });
  }

  formatParticipants(participants: HomeVisitParticipantDto[] | undefined): string {
    if (!participants?.length) return '—';
    return participants.map((p) => p.servantName).filter(Boolean).join('، ') || '—';
  }
}
