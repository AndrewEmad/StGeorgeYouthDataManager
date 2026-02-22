import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { StudentQueriesService } from '../../services/student-queries.service';
import { AssignmentRequestService } from '../../services/assignment-request.service';
import { AuthService } from '../../services/auth.service';
import { RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { LoaderComponent, EmptyStateComponent } from '../common/common';

export interface ServantStudentItem {
  student: any;
  segment: string;
}

@Component({
  selector: 'app-student-list',
  standalone: true,
  imports: [CommonModule, RouterLink, FormsModule, LoaderComponent, EmptyStateComponent],
  templateUrl: './student-list.html',
  styleUrls: ['./student-list.css']
})
export class StudentListComponent implements OnInit {
  items: ServantStudentItem[] = [];
  totalCount = 0;
  page = 1;
  pageSize = 20;
  searchTerm = '';
  filterArea = '';
  filterAcademicYear = '';
  filterGender: number | null = null;
  areas: string[] = [];
  academicYears: string[] = [];
  loading = true;
  requestingId: string | null = null;

  readonly genderOptions: { value: number; label: string }[] = [
    { value: 0, label: 'ذكر' },
    { value: 1, label: 'أنثى' }
  ];

  constructor(
    private studentQueriesService: StudentQueriesService,
    private assignmentRequestService: AssignmentRequestService,
    private authService: AuthService
  ) {}

  get currentUserId(): string | null {
    return this.authService.currentUser()?.userId ?? null;
  }

  get totalPages(): number {
    return Math.max(1, Math.ceil(this.totalCount / this.pageSize));
  }

  ngOnInit() {
    this.studentQueriesService.getDistinctAreas().subscribe({ next: (r) => { this.areas = Array.isArray(r) ? r : []; } });
    this.studentQueriesService.getDistinctAcademicYears().subscribe({ next: (r) => { this.academicYears = Array.isArray(r) ? r : []; } });
    this.loadPage();
  }

  loadPage() {
    const userId = this.currentUserId;
    if (!userId) {
      this.items = [];
      this.totalCount = 0;
      this.loading = false;
      return;
    }
    this.loading = true;
    this.studentQueriesService.getPagedForServant({
      page: this.page,
      pageSize: this.pageSize,
      search: this.searchTerm.trim() || undefined,
      area: this.filterArea.trim() || undefined,
      academicYear: this.filterAcademicYear.trim() || undefined,
      gender: this.filterGender ?? undefined
    }).subscribe({
      next: (res) => {
        this.items = res.items ?? [];
        this.totalCount = res.totalCount ?? 0;
        this.page = res.page ?? this.page;
        this.pageSize = res.pageSize ?? this.pageSize;
        this.loading = false;
      },
      error: () => {
        this.items = [];
        this.totalCount = 0;
        this.loading = false;
      }
    });
  }

  onSearch() {
    this.page = 1;
    this.loadPage();
  }

  onApplyFilters() {
    this.page = 1;
    this.loadPage();
  }

  clearFilters() {
    this.filterArea = '';
    this.filterAcademicYear = '';
    this.filterGender = null;
    this.page = 1;
    this.loadPage();
  }

  goToPage(p: number) {
    if (p < 1 || p > this.totalPages) return;
    this.page = p;
    this.loadPage();
  }

  requestAssignToMe(item: ServantStudentItem) {
    const student = item.student;
    if (!student?.id || this.requestingId || item.segment !== 'Unassigned') return;
    this.requestingId = student.id;
    this.assignmentRequestService.create(student.id).subscribe({
      next: () => {
        const idx = this.items.findIndex(i => i.student?.id === student.id);
        if (idx !== -1) this.items = this.items.slice(0, idx).concat([{ student, segment: 'Requested' }], this.items.slice(idx + 1));
        this.requestingId = null;
      },
      error: () => { this.requestingId = null; }
    });
  }
}
