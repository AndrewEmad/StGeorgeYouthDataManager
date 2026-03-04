import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AttendanceService } from '../../services/attendance.service';
import { StudentQueriesService } from '../../services/student-queries.service';
import { UsersService, User } from '../../services/users.service';
import { ContentHeaderComponent, CardComponent, LoaderComponent } from '../../components/common/common';

type Mode = 'students' | 'servants';

@Component({
  selector: 'app-attendance-page',
  standalone: true,
  imports: [CommonModule, FormsModule, ContentHeaderComponent, CardComponent, LoaderComponent],
  templateUrl: './attendance.page.html',
  styleUrls: ['./attendance.page.css']
})
export class AttendancePage implements OnInit {
  mode: Mode = 'students';
  attendanceDate = '';
  nameFilter = '';
  studentList: { id: string; fullName: string }[] = [];
  servantList: User[] = [];
  selectedStudentIds = new Set<string>();
  selectedServantIds = new Set<string>();
  selectedStudentEntries: { id: string; fullName: string }[] = [];
  selectedServantEntries: { id: string; fullName: string }[] = [];
  page = 1;
  pageSize = 20;
  totalCount = 0;
  loading = false;
  saving = false;
  error = '';
  success = '';

  constructor(
    private attendanceService: AttendanceService,
    private studentQueries: StudentQueriesService,
    private usersService: UsersService
  ) {}

  get totalPages(): number {
    return Math.max(1, Math.ceil(this.totalCount / this.pageSize));
  }

  get allOnPageSelected(): boolean {
    if (this.mode === 'students')
      return this.studentList.length > 0 && this.studentList.every((s) => this.selectedStudentIds.has(s.id));
    return this.servantList.length > 0 && this.servantList.every((s) => this.selectedServantIds.has(s.id));
  }

  ngOnInit() {
    this.loadPage();
  }

  setMode(m: Mode) {
    this.mode = m;
    this.selectedStudentIds.clear();
    this.selectedServantIds.clear();
    this.selectedStudentEntries = [];
    this.selectedServantEntries = [];
    this.page = 1;
    this.loadPage();
  }

  loadPage() {
    this.loading = true;
    this.error = '';
    this.success = '';
    if (this.mode === 'students') {
      this.studentQueries
        .getPaged({
          page: this.page,
          pageSize: this.pageSize,
          search: this.nameFilter.trim() || undefined,
          sortBy: 'fullName',
          sortDesc: false
        })
        .subscribe({
          next: (res) => {
            this.studentList = (res.items || []).map((s: any) => ({ id: s.id, fullName: s.fullName || '' }));
            this.studentList.sort((a, b) => (a.fullName || '').localeCompare(b.fullName || '', 'ar'));
            this.totalCount = res.totalCount ?? 0;
            this.page = res.page ?? this.page;
            this.pageSize = res.pageSize ?? this.pageSize;
            this.loading = false;
          },
          error: () => {
            this.error = 'فشل تحميل قائمة المخدومين';
            this.loading = false;
          }
        });
    } else {
      this.usersService
        .getPaged({ page: this.page, pageSize: this.pageSize, search: this.nameFilter.trim() || undefined, sortBy: 'fullName', sortDesc: false })
        .subscribe({
          next: (res) => {
            this.servantList = (res.items ?? []).slice().sort((a, b) => (a.fullName || '').localeCompare(b.fullName || '', 'ar'));
            this.totalCount = res.totalCount ?? 0;
            this.page = res.page ?? this.page;
            this.pageSize = res.pageSize ?? this.pageSize;
            this.loading = false;
          },
          error: () => {
            this.error = 'فشل تحميل قائمة الخدام';
            this.loading = false;
          }
        });
    }
  }

  onFilterChange() {
    this.page = 1;
    this.loadPage();
  }

  goPrev() {
    if (this.page > 1) {
      this.page--;
      this.loadPage();
    }
  }

  goNext() {
    if (this.page < this.totalPages) {
      this.page++;
      this.loadPage();
    }
  }

  toggleStudent(id: string, fullName?: string) {
    if (this.selectedStudentIds.has(id)) {
      this.selectedStudentIds.delete(id);
      this.selectedStudentEntries = this.selectedStudentEntries.filter((e) => e.id !== id);
    } else {
      this.selectedStudentIds.add(id);
      this.selectedStudentEntries = [...this.selectedStudentEntries, { id, fullName: fullName ?? '' }];
    }
    this.selectedStudentIds = new Set(this.selectedStudentIds);
  }

  toggleServant(id: string, fullName?: string) {
    if (this.selectedServantIds.has(id)) {
      this.selectedServantIds.delete(id);
      this.selectedServantEntries = this.selectedServantEntries.filter((e) => e.id !== id);
    } else {
      this.selectedServantIds.add(id);
      this.selectedServantEntries = [...this.selectedServantEntries, { id, fullName: fullName ?? '' }];
    }
    this.selectedServantIds = new Set(this.selectedServantIds);
  }

  selectAllStudents() {
    const allSelected = this.studentList.length > 0 && this.studentList.every((s) => this.selectedStudentIds.has(s.id));
    if (allSelected) {
      this.studentList.forEach((s) => {
        this.selectedStudentIds.delete(s.id);
        this.selectedStudentEntries = this.selectedStudentEntries.filter((e) => e.id !== s.id);
      });
    } else {
      this.studentList.forEach((s) => {
        if (!this.selectedStudentIds.has(s.id)) {
          this.selectedStudentIds.add(s.id);
          this.selectedStudentEntries = [...this.selectedStudentEntries, { id: s.id, fullName: s.fullName }];
        }
      });
    }
    this.selectedStudentIds = new Set(this.selectedStudentIds);
  }

  selectAllServants() {
    const allSelected = this.servantList.length > 0 && this.servantList.every((s) => this.selectedServantIds.has(s.id));
    if (allSelected) {
      this.servantList.forEach((s) => {
        this.selectedServantIds.delete(s.id);
        this.selectedServantEntries = this.selectedServantEntries.filter((e) => e.id !== s.id);
      });
    } else {
      this.servantList.forEach((s) => {
        if (!this.selectedServantIds.has(s.id)) {
          this.selectedServantIds.add(s.id);
          this.selectedServantEntries = [...this.selectedServantEntries, { id: s.id, fullName: s.fullName }];
        }
      });
    }
    this.selectedServantIds = new Set(this.selectedServantIds);
  }

  submit() {
    if (!this.attendanceDate?.trim()) {
      this.error = 'اختر التاريخ';
      return;
    }
    this.error = '';
    this.success = '';
    this.saving = true;
    if (this.mode === 'students') {
      if (this.selectedStudentIds.size === 0) {
        this.error = 'اختر مخدوم واحد على الأقل';
        this.saving = false;
        return;
      }
      this.attendanceService
        .recordStudentAttendance(this.attendanceDate, Array.from(this.selectedStudentIds))
        .subscribe({
          next: () => {
            this.success = 'تم تسجيل الحضور بنجاح';
            this.saving = false;
          },
          error: (err) => {
            this.error = err.error?.message || err.message || 'فشل تسجيل الحضور';
            this.saving = false;
          }
        });
    } else {
      if (this.selectedServantIds.size === 0) {
        this.error = 'اختر خادم واحد على الأقل';
        this.saving = false;
        return;
      }
      this.attendanceService
        .recordServantAttendance(this.attendanceDate, Array.from(this.selectedServantIds))
        .subscribe({
          next: () => {
            this.success = 'تم تسجيل حضور الخدام بنجاح';
            this.saving = false;
          },
          error: (err) => {
            this.error = err.error?.message || err.message || 'فشل تسجيل الحضور';
            this.saving = false;
          }
        });
    }
  }
}
