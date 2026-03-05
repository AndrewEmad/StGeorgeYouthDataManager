import { ChangeDetectionStrategy, Component, computed, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { AttendanceService } from '../../services/attendance.service';
import { StudentQueriesService } from '../../services/student-queries.service';
import { AuthService } from '../../core/services/auth.service';
import { LoaderComponent, CardComponent } from '../../shared/components';

interface StudentEntry {
  id: string;
  fullName: string;
}

@Component({
  selector: 'app-attendance-page',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [LoaderComponent, CardComponent],
  templateUrl: './attendance.page.html',
  styleUrls: ['./attendance.page.css'],
})
export class AttendancePage {
  private readonly attendanceService = inject(AttendanceService);
  private readonly studentQueries = inject(StudentQueriesService);
  private readonly auth = inject(AuthService);
  private readonly destroyRef = inject(DestroyRef);

  readonly attendanceDate = signal('');
  readonly nameFilter = signal('');
  readonly studentList = signal<StudentEntry[]>([]);
  readonly selectedStudentIds = signal<Set<string>>(new Set());
  readonly selectedStudentEntries = signal<StudentEntry[]>([]);
  readonly totalCount = signal(0);
  readonly page = signal(1);
  readonly pageSize = signal(20);
  readonly loading = signal(false);
  readonly saving = signal(false);
  readonly error = signal('');
  readonly success = signal('');

  readonly totalPages = computed(() => Math.max(1, Math.ceil(this.totalCount() / this.pageSize())));
  readonly allOnPageSelected = computed(() => {
    const list = this.studentList();
    const ids = this.selectedStudentIds();
    return list.length > 0 && list.every((s) => ids.has(s.id));
  });

  constructor() {
    this.loadPage();
  }

  loadPage(): void {
    const userId = this.auth.currentUser()?.userId;
    if (!userId) {
      this.studentList.set([]);
      this.totalCount.set(0);
      this.loading.set(false);
      return;
    }
    this.loading.set(true);
    this.error.set('');
    this.success.set('');
    this.studentQueries
      .getPagedForServant({
        page: this.page(),
        pageSize: this.pageSize(),
        search: this.nameFilter().trim() || undefined,
      })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (res) => {
          const list = (res.items || []).map((it: { student?: { id?: string; studentId?: string; fullName?: string } }) => ({
            id: it.student?.id ?? it.student?.studentId ?? '',
            fullName: it.student?.fullName ?? '',
          }));
          list.sort((a, b) => (a.fullName || '').localeCompare(b.fullName || '', 'ar'));
          this.studentList.set(list);
          this.totalCount.set(res.totalCount ?? 0);
          this.page.set(res.page ?? this.page());
          this.pageSize.set(res.pageSize ?? this.pageSize());
          this.loading.set(false);
        },
        error: () => {
          this.error.set('فشل تحميل قائمة المخدومين');
          this.loading.set(false);
        },
      });
  }

  onFilterChange(): void {
    this.page.set(1);
    this.loadPage();
  }

  goPrev(): void {
    if (this.page() > 1) {
      this.page.update((p) => p - 1);
      this.loadPage();
    }
  }

  goNext(): void {
    if (this.page() < this.totalPages()) {
      this.page.update((p) => p + 1);
      this.loadPage();
    }
  }

  toggleStudent(id: string, fullName?: string): void {
    const ids = new Set(this.selectedStudentIds());
    const entries = [...this.selectedStudentEntries()];
    if (ids.has(id)) {
      ids.delete(id);
      this.selectedStudentEntries.set(entries.filter((e) => e.id !== id));
    } else {
      ids.add(id);
      this.selectedStudentEntries.set([...entries, { id, fullName: fullName ?? '' }]);
    }
    this.selectedStudentIds.set(ids);
  }

  selectAllStudents(): void {
    const list = this.studentList();
    const allSelected = list.length > 0 && list.every((s) => this.selectedStudentIds().has(s.id));
    const ids = new Set(this.selectedStudentIds());
    let entries = [...this.selectedStudentEntries()];
    if (allSelected) {
      list.forEach((s) => {
        ids.delete(s.id);
        entries = entries.filter((e) => e.id !== s.id);
      });
    } else {
      list.forEach((s) => {
        if (!ids.has(s.id)) {
          ids.add(s.id);
          entries = [...entries, { id: s.id, fullName: s.fullName }];
        }
      });
    }
    this.selectedStudentIds.set(ids);
    this.selectedStudentEntries.set(entries);
  }

  submit(): void {
    const date = this.attendanceDate().trim();
    if (!date) {
      this.error.set('اختر التاريخ');
      return;
    }
    const ids = this.selectedStudentIds();
    if (ids.size === 0) {
      this.error.set('اختر مخدوم واحد على الأقل');
      return;
    }
    this.error.set('');
    this.success.set('');
    this.saving.set(true);
    this.attendanceService
      .recordStudentAttendance(date, Array.from(ids))
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.success.set('تم تسجيل الحضور بنجاح');
          this.saving.set(false);
        },
        error: (err) => {
          this.error.set(err.error?.message || err.message || 'فشل تسجيل الحضور');
          this.saving.set(false);
        },
      });
  }
}
