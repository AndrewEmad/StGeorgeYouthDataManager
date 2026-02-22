import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AttendanceService } from '../../services/attendance.service';
import { StudentQueriesService } from '../../services/student-queries.service';
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
  loading = false;
  saving = false;
  error = '';
  success = '';

  constructor(
    private attendanceService: AttendanceService,
    private studentQueries: StudentQueriesService
  ) {}

  ngOnInit() {
    this.load();
  }

  load() {
    this.loading = true;
    this.error = '';
    this.success = '';
    this.studentQueries
      .getPaged({
        page: 1,
        pageSize: 500,
        search: this.nameFilter.trim() || undefined
      })
      .subscribe({
        next: (res) => {
          this.studentList = (res.items || []).map((s: any) => ({ id: s.id, fullName: s.fullName || '' }));
          this.loading = false;
        },
        error: () => {
          this.error = 'فشل تحميل قائمة المخدومين';
          this.loading = false;
        }
      });
  }

  onFilterChange() {
    this.load();
  }

  toggleStudent(id: string) {
    if (this.selectedStudentIds.has(id)) this.selectedStudentIds.delete(id);
    else this.selectedStudentIds.add(id);
    this.selectedStudentIds = new Set(this.selectedStudentIds);
  }

  selectAllStudents() {
    if (this.selectedStudentIds.size === this.studentList.length)
      this.selectedStudentIds.clear();
    else this.studentList.forEach((s) => this.selectedStudentIds.add(s.id));
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
