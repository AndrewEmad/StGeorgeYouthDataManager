import { Component, OnInit, signal, computed, inject, ChangeDetectionStrategy } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AttendanceService } from '../../services/attendance.service';
import { StudentQueriesService } from '../../services/student-queries.service';
import { UsersService } from '../../services/users.service';
import { User } from '../../shared/models';
import { ContentHeaderComponent, CardComponent, LoaderComponent } from '../../components/common/common';

type Mode = 'students' | 'servants';

@Component({
  selector: 'app-attendance-page',
  standalone: true,
  imports: [FormsModule, ContentHeaderComponent, CardComponent, LoaderComponent],
  templateUrl: './attendance.page.html',
  styleUrls: ['./attendance.page.css'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AttendancePage implements OnInit {
  private attendanceService = inject(AttendanceService);
  private studentQueries = inject(StudentQueriesService);
  private usersService = inject(UsersService);

  mode = signal<Mode>('students');
  attendanceDate = signal('');
  nameFilter = signal('');
  studentList = signal<{ id: string; fullName: string }[]>([]);
  servantList = signal<User[]>([]);
  selectedStudentIds = signal<Set<string>>(new Set());
  selectedServantIds = signal<Set<string>>(new Set());
  selectedStudentEntries = signal<{ id: string; fullName: string }[]>([]);
  selectedServantEntries = signal<{ id: string; fullName: string }[]>([]);
  page = signal(1);
  pageSize = signal(20);
  totalCount = signal(0);
  loading = signal(false);
  saving = signal(false);
  error = signal('');
  success = signal('');

  totalPages = computed(() => Math.max(1, Math.ceil(this.totalCount() / this.pageSize())));

  allOnPageSelected = computed(() => {
    const mode = this.mode();
    if (mode === 'students') {
      const list = this.studentList();
      const ids = this.selectedStudentIds();
      return list.length > 0 && list.every((s) => ids.has(s.id));
    }
    const list = this.servantList();
    const ids = this.selectedServantIds();
    return list.length > 0 && list.every((s) => ids.has(s.id ?? ''));
  });

  ngOnInit(): void {
    this.loadPage();
  }

  setMode(m: Mode): void {
    this.mode.set(m);
    this.selectedStudentIds.set(new Set());
    this.selectedServantIds.set(new Set());
    this.selectedStudentEntries.set([]);
    this.selectedServantEntries.set([]);
    this.page.set(1);
    this.loadPage();
  }

  loadPage(): void {
    this.loading.set(true);
    this.error.set('');
    this.success.set('');
    const mode = this.mode();
    if (mode === 'students') {
      this.studentQueries
        .getPaged({
          page: this.page(),
          pageSize: this.pageSize(),
          search: this.nameFilter().trim() || undefined,
          sortBy: 'fullName',
          sortDesc: false,
        })
        .subscribe({
          next: (res) => {
            const items = (res.items || []).map((s: { id?: string; fullName?: string }) => ({
              id: s.id ?? '',
              fullName: s.fullName ?? '',
            }));
            items.sort((a, b) => (a.fullName || '').localeCompare(b.fullName || '', 'ar'));
            this.studentList.set(items);
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
    } else {
      this.usersService
        .getPaged({
          page: this.page(),
          pageSize: this.pageSize(),
          search: this.nameFilter().trim() || undefined,
          sortBy: 'fullName',
          sortDesc: false,
        })
        .subscribe({
          next: (res) => {
            const items = (res.items ?? []).slice().sort((a, b) =>
              (a.fullName || '').localeCompare(b.fullName || '', 'ar')
            );
            this.servantList.set(items);
            this.totalCount.set(res.totalCount ?? 0);
            this.page.set(res.page ?? this.page());
            this.pageSize.set(res.pageSize ?? this.pageSize());
            this.loading.set(false);
          },
          error: () => {
            this.error.set('فشل تحميل قائمة الخدام');
            this.loading.set(false);
          },
        });
    }
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

  toggleServant(id: string, fullName?: string): void {
    const ids = new Set(this.selectedServantIds());
    const entries = [...this.selectedServantEntries()];
    if (ids.has(id)) {
      ids.delete(id);
      this.selectedServantEntries.set(entries.filter((e) => e.id !== id));
    } else {
      ids.add(id);
      this.selectedServantEntries.set([...entries, { id, fullName: fullName ?? '' }]);
    }
    this.selectedServantIds.set(ids);
  }

  selectAllStudents(): void {
    const list = this.studentList();
    const ids = this.selectedStudentIds();
    const allSelected = list.length > 0 && list.every((s) => ids.has(s.id));
    const newIds = new Set(ids);
    let newEntries = [...this.selectedStudentEntries()];
    if (allSelected) {
      list.forEach((s) => {
        newIds.delete(s.id);
        newEntries = newEntries.filter((e) => e.id !== s.id);
      });
    } else {
      list.forEach((s) => {
        if (!newIds.has(s.id)) {
          newIds.add(s.id);
          newEntries = [...newEntries, { id: s.id, fullName: s.fullName }];
        }
      });
    }
    this.selectedStudentIds.set(newIds);
    this.selectedStudentEntries.set(newEntries);
  }

  selectAllServants(): void {
    const list = this.servantList();
    const ids = this.selectedServantIds();
    const allSelected = list.length > 0 && list.every((s) => ids.has(s.id ?? ''));
    const newIds = new Set(ids);
    let newEntries = [...this.selectedServantEntries()];
    if (allSelected) {
      list.forEach((s) => {
        const id = s.id ?? '';
        newIds.delete(id);
        newEntries = newEntries.filter((e) => e.id !== id);
      });
    } else {
      list.forEach((s) => {
        const id = s.id ?? '';
        if (!newIds.has(id)) {
          newIds.add(id);
          newEntries = [...newEntries, { id, fullName: s.fullName ?? '' }];
        }
      });
    }
    this.selectedServantIds.set(newIds);
    this.selectedServantEntries.set(newEntries);
  }

  submit(): void {
    const date = this.attendanceDate()?.trim();
    if (!date) {
      this.error.set('اختر التاريخ');
      return;
    }
    this.error.set('');
    this.success.set('');
    this.saving.set(true);
    const mode = this.mode();
    if (mode === 'students') {
      const ids = this.selectedStudentIds();
      if (ids.size === 0) {
        this.error.set('اختر مخدوم واحد على الأقل');
        this.saving.set(false);
        return;
      }
      this.attendanceService.recordStudentAttendance(date, Array.from(ids)).subscribe({
        next: () => {
          this.success.set('تم تسجيل الحضور بنجاح');
          this.saving.set(false);
        },
        error: (err: { error?: { message?: string }; message?: string }) => {
          this.error.set(err.error?.message || err.message || 'فشل تسجيل الحضور');
          this.saving.set(false);
        },
      });
    } else {
      const ids = this.selectedServantIds();
      if (ids.size === 0) {
        this.error.set('اختر خادم واحد على الأقل');
        this.saving.set(false);
        return;
      }
      this.attendanceService.recordServantAttendance(date, Array.from(ids)).subscribe({
        next: () => {
          this.success.set('تم تسجيل حضور الخدام بنجاح');
          this.saving.set(false);
        },
        error: (err: { error?: { message?: string }; message?: string }) => {
          this.error.set(err.error?.message || err.message || 'فشل تسجيل الحضور');
          this.saving.set(false);
        },
      });
    }
  }
}
