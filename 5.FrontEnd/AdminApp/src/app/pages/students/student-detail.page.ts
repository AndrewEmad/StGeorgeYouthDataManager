import {
  Component,
  inject,
  signal,
  ChangeDetectionStrategy,
  DestroyRef,
  OnInit,
  AfterViewInit,
  viewChild,
  TemplateRef,
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
import { DataTableComponent } from '../../shared/components/data-table/data-table.component';
import type { DataTableColumn } from '../../shared/components/data-table/data-table-column';
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
    DataTableComponent,
    DateFormatPipe,
    CallStatusPipe,
    VisitOutcomePipe,
  ],
  templateUrl: './student-detail.page.html',
  styleUrls: ['./student-detail.page.css'],
})
export class StudentDetailPage implements OnInit, AfterViewInit {
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

  readonly editHistoryColumns: DataTableColumn<StudentEditLogDto>[] = [
    { key: 'updatedAt', header: 'التاريخ', format: 'date' },
    { key: 'updatedByUserName', header: 'من قام بالتعديل' },
    { key: 'details', header: 'التفاصيل' },
  ];

  callsColumns = signal<DataTableColumn<CallLogDto>[]>([]);
  visitsColumns = signal<DataTableColumn<HomeVisitDto>[]>([]);

  private callStatusCellTpl = viewChild<TemplateRef<{ $implicit: CallLogDto; value: unknown }>>('callStatusCell');
  private visitOutcomeCellTpl = viewChild<TemplateRef<{ $implicit: HomeVisitDto; value: unknown }>>('visitOutcomeCell');
  private participantsCellTpl = viewChild<TemplateRef<{ $implicit: HomeVisitDto; value: unknown }>>('participantsCell');

  constructor() {
    this.destroyRef.onDestroy(() => {
      const url = this.studentPhotoUrl();
      if (url) URL.revokeObjectURL(url);
    });
  }

  ngAfterViewInit(): void {
    const callStatusTpl = this.callStatusCellTpl();
    const visitOutcomeTpl = this.visitOutcomeCellTpl();
    const participantsTpl = this.participantsCellTpl();
    if (callStatusTpl) {
      this.callsColumns.set([
        { key: 'callDate', header: 'التاريخ', format: 'date' },
        { key: 'callStatus', header: 'الحالة', template: callStatusTpl },
        { key: 'notes', header: 'ملاحظات' },
        { key: 'nextFollowUpDate', header: 'متابعة قادمة', format: 'date' },
      ]);
    }
    if (visitOutcomeTpl && participantsTpl) {
      this.visitsColumns.set([
        { key: 'visitDate', header: 'التاريخ', format: 'date' },
        { key: 'visitOutcome', header: 'النتيجة', template: visitOutcomeTpl },
        { key: 'participants', header: 'المرافقين', template: participantsTpl },
        { key: 'notes', header: 'ملاحظات' },
        { key: 'nextVisitDate', header: 'زيارة قادمة', format: 'date' },
      ]);
    }
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

  formatParticipants(participants: HomeVisitParticipantDto[] | undefined | unknown): string {
    const list = Array.isArray(participants) ? participants : undefined;
    if (!list?.length) return '—';
    return list.map((p: HomeVisitParticipantDto) => p.servantName).filter(Boolean).join('، ') || '—';
  }
}
