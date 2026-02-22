import { Component, OnInit, ViewChild, ElementRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, ActivatedRoute } from '@angular/router';
import { forkJoin } from 'rxjs';
import { StudentQueriesService } from '../../../services/student-queries.service';
import { UsersService, User } from '../../../services/users.service';
import { StudentCommandsService, CreateStudentRequest, UpdateStudentRequest } from '../../../services/student-commands.service';
import {
  ContentHeaderComponent,
  CardComponent,
  FiltersBarComponent,
  FormFieldComponent,
  LoaderComponent,
  PaginationComponent
} from '../../common/common';

@Component({
  selector: 'app-student-list-admin',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    ContentHeaderComponent,
    CardComponent,
    FiltersBarComponent,
    FormFieldComponent,
    LoaderComponent,
    PaginationComponent
  ],
  templateUrl: './student-list-admin.html',
  styleUrls: ['./student-list-admin.css']
})
export class StudentListAdminComponent implements OnInit {
  students: any[] = [];
  totalCount = 0;
  page = 1;
  pageSize = 10;
  pageSizeOptions = [5, 10, 20, 50];
  servants: User[] = [];
  loading = true;
  searchTerm = '';
  selectedStudent: any = null;
  showAssignModal = false;
  targetServantId = '';
  selectedStudentIds = new Set<string>();
  showBulkAssignModal = false;
  bulkTargetServantId = '';
  bulkAssignSaving = false;
  showAddModal = false;
  addSaving = false;
  showBulkUploadModal = false;
  uploadingCsv = false;
  uploadResult: { created: number; errors: { row: number; message: string }[] } | null = null;
  showFilters = false;
  filters = {
    search: '',
    area: '',
    academicYear: '',
    gender: '' as '' | '0' | '1',
    servantId: '',
    hasServant: '' as '' | 'yes' | 'no'
  };
  regionOptions: string[] = [];
  academicYearOptions: string[] = [];
  sortBy: string | null = 'fullName';
  sortDesc = false;
  newStudent: CreateStudentRequest = {
    fullName: '',
    phone: '',
    address: null,
    area: null,
    college: null,
    academicYear: null,
    confessionFather: null,
    gender: 0,
    servantId: null,
    birthDate: null
  };

  constructor(
    private studentQueries: StudentQueriesService,
    private studentCommands: StudentCommandsService,
    private usersService: UsersService,
    private router: Router,
    private route: ActivatedRoute
  ) {}

  ngOnInit() {
    this.route.queryParams.subscribe((params) => {
      this.filters.area = params['area'] ?? '';
      this.filters.academicYear = params['academicYear'] ?? '';
      if (this.filters.area || this.filters.academicYear) this.showFilters = true;
      this.page = 1;
      this.loadData();
    });
    this.loadOptionsAndServants();
  }

  loadData() {
    const genderNum = this.filters.gender === '' ? null : Number(this.filters.gender);
    const hasServant = this.filters.hasServant === 'yes' ? true : this.filters.hasServant === 'no' ? false : null;
    const servantId = this.filters.servantId === '_none' || !this.filters.servantId ? null : this.filters.servantId;
    this.studentQueries.getPaged({
      page: this.page,
      pageSize: this.pageSize,
      search: this.filters.search.trim() || undefined,
      area: this.filters.area || undefined,
      academicYear: this.filters.academicYear || undefined,
      gender: genderNum ?? undefined,
      servantId: servantId || undefined,
      hasServant: hasServant ?? undefined,
      sortBy: this.sortBy || 'fullName',
      sortDesc: this.sortDesc
    }).subscribe({
      next: (res) => {
        this.students = res.items;
        this.totalCount = res.totalCount;
        this.page = res.page;
        this.loading = false;
      },
      error: () => this.loading = false
    });
  }

  loadOptionsAndServants() {
    this.studentQueries.getDistinctAreas().subscribe({
      next: (areas) => { this.regionOptions = areas || []; }
    });
    this.studentQueries.getDistinctAcademicYears().subscribe({
      next: (years) => { this.academicYearOptions = years || []; }
    });
    this.usersService.getAll().subscribe(data => {
      this.servants = data.filter(u => u.role !== 'Priest' && (String(u.role) === 'Servant' || (u as any).role === 0 || u.role === 'Manager'));
    });
  }

  applyFilters() {
    this.page = 1;
    this.loadData();
  }

  clearFilters() {
    this.filters = { search: '', area: '', academicYear: '', gender: '', servantId: '', hasServant: '' };
    this.page = 1;
    this.loadData();
  }

  get totalPages(): number {
    return Math.max(1, Math.ceil(this.totalCount / this.pageSize));
  }

  goToPage(p: number) {
    if (p < 1 || p > this.totalPages) return;
    this.page = p;
    this.loadData();
  }

  onPageSizeChange() {
    this.page = 1;
    this.loadData();
  }

  setPageSize(n: number) {
    this.pageSize = Number(n);
    this.onPageSizeChange();
  }

  setSort(column: string) {
    if (this.sortBy === column) this.sortDesc = !this.sortDesc;
    else { this.sortBy = column; this.sortDesc = false; }
    this.page = 1;
    this.loadData();
  }

  viewStudent(s: any) {
    this.router.navigate(['/dashboard/students', s.id]);
  }

  @ViewChild('csvFileInput') csvFileInputRef?: ElementRef<HTMLInputElement>;

  openBulkUploadModal() {
    this.showBulkUploadModal = true;
  }

  closeBulkUploadModal() {
    this.showBulkUploadModal = false;
  }

  triggerCsvFileSelect() {
    this.csvFileInputRef?.nativeElement?.click();
  }

  downloadCsvTemplate() {
    this.studentCommands.downloadCsvTemplate();
  }

  onCsvFileSelected(event: Event) {
    this.showBulkUploadModal = false;
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    input.value = '';
    if (!file) return;
    this.uploadingCsv = true;
    this.uploadResult = null;
    const reader = new FileReader();
    reader.onload = () => {
      const text = (reader.result as string) || '';
      const items = this.studentCommands.parseCsvToStudents(text);
      if (items.length === 0) {
        this.uploadingCsv = false;
        this.uploadResult = { created: 0, errors: [{ row: 0, message: 'لم يتم العثور على صفوف صالحة (الاسم ورقم التليفون مطلوبان)' }] };
        return;
      }
      this.studentCommands.bulkImport(items).subscribe({
        next: (res) => {
          this.uploadingCsv = false;
          this.uploadResult = { created: res.created, errors: res.errors || [] };
          this.loadData();
        },
        error: (err) => {
          this.uploadingCsv = false;
          this.uploadResult = { created: 0, errors: [{ row: 0, message: err.error?.message || err.message || 'خطأ في الاستيراد' }] };
        }
      });
    };
    reader.readAsText(file, 'UTF-8');
  }

  openAddModal() {
    this.newStudent = {
      fullName: '',
      phone: '',
      address: null,
      area: null,
      college: null,
      academicYear: null,
      confessionFather: null,
      gender: 0,
      servantId: null,
      birthDate: null
    };
    this.showAddModal = true;
  }

  onCreateStudent() {
    if (!this.newStudent.fullName?.trim() || !this.newStudent.phone?.trim()) {
      alert('الاسم ورقم التليفون مطلوبان');
      return;
    }
    this.addSaving = true;
    const req: CreateStudentRequest = {
      ...this.newStudent,
      servantId: this.newStudent.servantId || null,
      birthDate: this.newStudent.birthDate || null
    };
    this.studentCommands.create(req).subscribe({
      next: () => {
        this.showAddModal = false;
        this.addSaving = false;
        this.loadData();
      },
      error: () => {
        this.addSaving = false;
        alert('خطأ في إضافة المخدوم');
      }
    });
  }

  openAssignModal(student: any) {
    this.selectedStudent = student;
    this.targetServantId = student.servantId || '';
    this.showAssignModal = true;
  }

  isSelected(s: any): boolean {
    return this.selectedStudentIds.has(s.id);
  }

  toggleSelection(s: any) {
    if (this.selectedStudentIds.has(s.id)) this.selectedStudentIds.delete(s.id);
    else this.selectedStudentIds.add(s.id);
    this.selectedStudentIds = new Set(this.selectedStudentIds);
  }

  selectAllFiltered() {
    const all = this.students.every((s: any) => this.selectedStudentIds.has(s.id));
    if (all) this.selectedStudentIds.clear();
    else this.students.forEach((s: any) => this.selectedStudentIds.add(s.id));
    this.selectedStudentIds = new Set(this.selectedStudentIds);
  }

  get allFilteredSelected(): boolean {
    return this.students.length > 0 && this.students.every((s: any) => this.selectedStudentIds.has(s.id));
  }

  openBulkAssignModal() {
    if (this.selectedStudentIds.size === 0) return;
    this.bulkTargetServantId = '';
    this.showBulkAssignModal = true;
  }

  onBulkAssign() {
    if (!this.bulkTargetServantId || this.selectedStudentIds.size === 0) return;
    this.bulkAssignSaving = true;
    const ids = Array.from(this.selectedStudentIds);
    const requests = ids.map(id => this.studentCommands.assignToServant(id, this.bulkTargetServantId));
    forkJoin(requests).subscribe({
      next: () => {
        this.bulkAssignSaving = false;
        this.showBulkAssignModal = false;
        this.selectedStudentIds.clear();
        this.selectedStudentIds = new Set(this.selectedStudentIds);
        this.loadData();
      },
      error: () => {
        this.bulkAssignSaving = false;
        alert('حدث خطأ في تخصيص بعض المخدومين');
      }
    });
  }

  deleteStudent(s: any) {
    if (!confirm(`حذف المخدوم «${s.fullName}»؟ لا يمكن التراجع.`)) return;
    this.studentCommands.delete(s.id).subscribe({
      next: () => {
        this.loadData();
      },
      error: (err: any) => alert('خطأ في الحذف: ' + (err.error?.message || err.message))
    });
  }

  onAssign() {
    if (!this.targetServantId) return;

    if (this.targetServantId === '_none') {
      const req: UpdateStudentRequest = {
        id: this.selectedStudent.id,
        fullName: this.selectedStudent.fullName,
        phone: this.selectedStudent.phone,
        address: this.selectedStudent.address ?? null,
        area: this.selectedStudent.area ?? null,
        college: this.selectedStudent.college ?? null,
        academicYear: this.selectedStudent.academicYear ?? null,
        confessionFather: this.selectedStudent.confessionFather ?? null,
        gender: this.selectedStudent.gender ?? 0,
        servantId: null,
        birthDate: this.selectedStudent.birthDate || null
      };
      this.studentCommands.update(req).subscribe({
        next: () => {
          this.showAssignModal = false;
          this.loadData();
        },
        error: (err) => alert('خطأ: ' + (err.error?.message || 'فشل إلغاء التخصيص'))
      });
      return;
    }

    this.studentCommands.assignToServant(this.selectedStudent.id, this.targetServantId).subscribe({
      next: () => {
        this.showAssignModal = false;
        this.loadData();
      },
      error: (err) => alert('خطأ في التخصيص')
    });
  }
}
