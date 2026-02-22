import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AttendanceService } from '../../services/attendance.service';
import { StudentQueriesService } from '../../services/student-queries.service';
import { AuthService } from '../../services/auth.service';
import { LoaderComponent, CardComponent } from '../../components/common/common';

@Component({
  selector: 'app-attendance-page',
  standalone: true,
  imports: [CommonModule, FormsModule, LoaderComponent, CardComponent],
  templateUrl: './attendance.page.html',
  styleUrls: ['./attendance.page.css']
})
export class AttendancePage implements OnInit {
  attendanceDate = '';
  nameFilter = '';
  studentList: { id: string; fullName: string }[] = [];
  selectedStudentIds = new Set<string>();
  selectedStudentEntries: { id: string; fullName: string }[] = [];
  totalCount = 0;
  page = 1;
  pageSize = 20;
  loading = false;
  saving = false;
  error = '';
  success = '';

  constructor(
    private attendanceService: AttendanceService,
    private studentQueries: StudentQueriesService,
    private auth: AuthService
  ) {}

  get totalPages(): number {
    return Math.max(1, Math.ceil(this.totalCount / this.pageSize));
  }

  get allOnPageSelected(): boolean {
    return this.studentList.length > 0 && this.studentList.every((s) => this.selectedStudentIds.has(s.id));
  }

  ngOnInit() {
    this.loadPage();
  }

  loadPage() {
    const userId = this.auth.currentUser()?.userId;
    if (!userId) {
      this.studentList = [];
      this.totalCount = 0;
      this.loading = false;
      return;
    }
    this.loading = true;
    this.error = '';
    this.success = '';
    this.studentQueries
      .getPagedForServant({
        page: this.page,
        pageSize: this.pageSize,
        search: this.nameFilter.trim() || undefined
      })
      .subscribe({
        next: (res) => {
          this.studentList = (res.items || []).map((it: any) => ({
            id: it.student?.id ?? it.student?.studentId ?? '',
            fullName: it.student?.fullName ?? ''
          }));
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

  selectAllStudents() {
    const allOnPageSelected = this.studentList.length > 0 && this.studentList.every((s) => this.selectedStudentIds.has(s.id));
    if (allOnPageSelected) {
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

  submit() {
    if (!this.attendanceDate?.trim()) {
      this.error = 'اختر التاريخ';
      return;
    }
    if (this.selectedStudentIds.size === 0) {
      this.error = 'اختر مخدوم واحد على الأقل';
      return;
    }
    this.error = '';
    this.success = '';
    this.saving = true;
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
  }
}
