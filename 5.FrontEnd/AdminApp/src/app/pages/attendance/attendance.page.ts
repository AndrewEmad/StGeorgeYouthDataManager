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
  loading = false;
  saving = false;
  error = '';
  success = '';

  constructor(
    private attendanceService: AttendanceService,
    private studentQueries: StudentQueriesService,
    private usersService: UsersService
  ) {}

  ngOnInit() {
    this.load();
  }

  setMode(m: Mode) {
    this.mode = m;
    this.selectedStudentIds.clear();
    this.selectedServantIds.clear();
    this.load();
  }

  load() {
    this.loading = true;
    this.error = '';
    this.success = '';
    if (this.mode === 'students') {
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
    } else {
      this.usersService
        .getPaged({ page: 1, pageSize: 500, search: this.nameFilter.trim() || undefined })
        .subscribe({
          next: (res) => {
            this.servantList = res.items ?? [];
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
    this.load();
  }

  toggleStudent(id: string) {
    if (this.selectedStudentIds.has(id)) this.selectedStudentIds.delete(id);
    else this.selectedStudentIds.add(id);
    this.selectedStudentIds = new Set(this.selectedStudentIds);
  }

  toggleServant(id: string) {
    if (this.selectedServantIds.has(id)) this.selectedServantIds.delete(id);
    else this.selectedServantIds.add(id);
    this.selectedServantIds = new Set(this.selectedServantIds);
  }

  selectAllStudents() {
    if (this.selectedStudentIds.size === this.studentList.length)
      this.selectedStudentIds.clear();
    else this.studentList.forEach((s) => this.selectedStudentIds.add(s.id));
    this.selectedStudentIds = new Set(this.selectedStudentIds);
  }

  selectAllServants() {
    if (this.selectedServantIds.size === this.servantList.length)
      this.selectedServantIds.clear();
    else this.servantList.forEach((s) => this.selectedServantIds.add(s.id));
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
