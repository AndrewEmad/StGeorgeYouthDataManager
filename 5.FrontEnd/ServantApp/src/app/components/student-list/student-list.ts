import {studentAreas} from '@ydm/shared/core/student-areas';
import {Subscription} from 'rxjs';
import {apiError} from '@ydm/shared/core/api';
import { phoneInternational } from '@ydm/shared/core/utils';
import { ChangeDetectionStrategy, Component, computed, DestroyRef, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { StudentQueriesService } from '../../services/student-queries.service';
import { AssignmentRequestService } from '../../services/assignment-request.service';
import { AuthService } from '../../core/services/auth.service';
import { LoaderComponent, EmptyStateComponent } from '../../shared/components';
import type { ServantStudentItem } from '../../shared/models/student.model';
import { GENDER_OPTIONS } from '../../shared/models/student.model';

@Component({
  selector: 'app-student-list',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [DatePipe, RouterLink, LoaderComponent, EmptyStateComponent],
  templateUrl: './student-list.html',
  styleUrls: ['./student-list.css'],
})
export class StudentListComponent {
  private readonly studentQueriesService = inject(StudentQueriesService);
  private readonly assignmentRequestService = inject(AssignmentRequestService);
  private readonly authService = inject(AuthService);
  private readonly destroyRef = inject(DestroyRef);

  private request?:Subscription;phone=phoneInternational;readonly error=signal('');readonly message=signal('');
  readonly items = signal<ServantStudentItem[]>([]);
  readonly totalCount = signal(0);
  readonly page = signal(1);
  readonly pageSize = signal(25);
  readonly searchTerm = signal('');
  readonly segment = signal('Assigned');
  readonly filterArea = signal('');
  readonly filterAcademicYear = signal('');
  readonly filterGender = signal<number | null>(null);
  readonly areas = signal<string[]>([...studentAreas]);
  readonly academicYears = signal<string[]>([]);
  readonly loading = signal(true);
  readonly requestingId = signal<string | null>(null);

  readonly genderOptions = GENDER_OPTIONS;
  readonly currentUserId = computed(() => this.authService.currentUser()?.userId ?? null);
  readonly totalPages = computed(() => Math.max(1, Math.ceil(this.totalCount() / this.pageSize())));

  constructor() {
    this.destroyRef.onDestroy(()=>this.request?.unsubscribe());
    this.studentQueriesService
      .getDistinctAcademicYears()
      .pipe(takeUntilDestroyed())
      .subscribe({next:r=>this.academicYears.set(Array.isArray(r) ? r : []),error:e=>this.error.set(apiError(e))});
    this.loadPage();
  }

  loadPage(): void {
    const userId = this.currentUserId();
    if (!userId) {
      this.items.set([]);
      this.totalCount.set(0);
      this.loading.set(false);
      return;
    }
    this.request?.unsubscribe();this.loading.set(true);this.error.set('');
    this.request=this.studentQueriesService
      .getPagedForServant({
        segment:this.segment(),
        page: this.page(),
        pageSize: this.pageSize(),
        search: this.searchTerm().trim() || undefined,
        area: this.filterArea().trim() || undefined,
        academicYear: this.filterAcademicYear().trim() || undefined,
        gender: this.filterGender() ?? undefined,
      })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (res) => {
          this.items.set(res.items ?? []);
          this.totalCount.set(res.totalCount ?? 0);
          this.page.set(res.page ?? this.page());
          this.pageSize.set(res.pageSize ?? this.pageSize());
          this.loading.set(false);
        },
        error: e => {
          this.error.set(apiError(e));
          this.items.set([]);
          this.totalCount.set(0);
          this.loading.set(false);
        },
      });
  }

  onApplyFilters(): void {
    this.page.set(1);
    this.loadPage();
  }

  clearFilters(): void {
    this.segment.set('Assigned');
    this.searchTerm.set('');
    this.filterArea.set('');
    this.filterAcademicYear.set('');
    this.filterGender.set(null);
    this.page.set(1);
    this.loadPage();
  }

  goToPage(p: number): void {
    if (p < 1 || p > this.totalPages()) return;
    this.page.set(p);
    this.loadPage();
  }

  requestAssignToMe(item: ServantStudentItem): void {
    const student = item.student;
    if (!student?.id || this.requestingId() || item.segment !== 'Unassigned') return;
    this.requestingId.set(student.id);
    this.assignmentRequestService
      .create(student.id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          const idx = this.items().findIndex((i) => i.student?.id === student.id);
          if (idx !== -1)
            this.items.update((list) =>
              list.slice(0, idx).concat([{ student, segment: 'Requested' }], list.slice(idx + 1))
            );
          this.requestingId.set(null);this.message.set('تم إرسال طلب الإسناد للمراجعة.');
        },
        error: e=>{this.requestingId.set(null);this.error.set(apiError(e));},
      });
  }

  getWaMeUrl(phone: string): string {
    const digits = phoneInternational(phone);
    if (!digits) return '#';
    return `https://wa.me/${digits}`;
  }
}
