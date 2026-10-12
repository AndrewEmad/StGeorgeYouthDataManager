import { apiError } from '@ydm/shared/core/api';
import { dateOnly } from '@ydm/shared/core/utils';
import { ResourcePickerComponent } from '@ydm/shared/ui/resource-picker';
import {
  AfterViewInit,
  Component,
  ElementRef,
  OnInit,
  TemplateRef,
  ViewChild,
  signal,
  viewChild,
} from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router,RouterLink } from '@angular/router';
import { Subscription } from 'rxjs';
import {
  ModalComponent,
  CardComponent,
  ContentHeaderComponent,
  FiltersBarComponent,
  FormFieldComponent,
  LoaderComponent,
  PhotoCropModalComponent,
  ProfilePhotoInputComponent,
} from '../../../components/common/common';
import { StudentCommandsService, CsvStudentRow } from '../../../services/student-commands.service';
import { StudentQueriesService } from '../../../services/student-queries.service';
import { UsersService } from '../../../services/users.service';
import type { DataTableColumn } from '../../../shared/components/data-table/data-table-column';
import { DataTableComponent } from '../../../shared/components/data-table/data-table.component';
import { CreateStudentRequest, UpdateStudentRequest, User } from '../../../shared/models';

/** Shape of a student row in the list (from getPaged) */
interface StudentListItem {
  id: string;
  fullName: string;
  academicYear?: string | null;
  servantName?: string | null;
  servantId?: string | null;
  lastFollowUpDate?: string | null;
  lastAttendanceDate?: string | null;
  lastFollowUpNote?: string | null;
}

@Component({
  selector: 'app-student-list-admin',
  standalone: true,
  imports: [
    FormsModule,RouterLink,
    ResourcePickerComponent,
    ContentHeaderComponent,
    ModalComponent,
  CardComponent,
    FiltersBarComponent,
    FormFieldComponent,
    LoaderComponent,
    PhotoCropModalComponent,
    ProfilePhotoInputComponent,
    DataTableComponent,
  ],
  templateUrl: './student-list-admin.html',
  styleUrls: ['./student-list-admin.css'],
})
export class StudentListAdminComponent implements OnInit, AfterViewInit {
  students: StudentListItem[] = [];
  totalCount = 0;
  page = 1;
  pageSize = 25;
  pageSizeOptions = [10,25,50,100];
  listError="";
  private listRequest?:Subscription;
  private searchTimer?:ReturnType<typeof setTimeout>;
  private scopeKey="";
  servants: User[] = [];
  loading = true;
  selectedStudent: any = null;
  showAssignModal = false;
  targetServantId = '';
  selectedStudentIds = new Set<string>();
  showBulkAssignModal = false;
  bulkTargetServantId = '';
  bulkAssignSaving = false;
  showAddModal = false;
  addSaving = false;
  showEditModal = false;
  editingStudent: any = null;
  assignSaving=false;
  formMessage="";
  photoRetry:{id:string;file:File}|null=null;
  photoRetryBusy=false;
  birthMonths=Array.from({length:12},(_,i)=>i+1);
  editForm: {
    fullName: string;
    phone: string;
    secondaryPhone:string|null;
    birthMonth:number|null;
    notes:string|null;
    address: string | null;
    area: string | null;
    college: string | null;
    academicYear: string | null;
    confessionFather: string | null;
    gender: number;
    servantId: string | null;
    birthDate: string | null;
  } = {
    fullName: '',
    phone: '',
    secondaryPhone:null,birthMonth:null,notes:null,
    address: null,
    area: null,
    college: null,
    academicYear: null,
    confessionFather: null,
    gender: 0,
    servantId: null,
    birthDate: null,
  };
  editStudentPhoto: File | null = null;
  editStudentPhotoPreviewUrl: string | null = null;
  editStudentExistingPhotoUrl: string | null = null;
  editSaving = false;
  csvPreview: CsvStudentRow[] | null = null;
  csvOperationId = "";csvReviewed=false;csvReviewError="";
  private csvReader?: FileReader;
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
    hasServant: '' as '' | 'yes' | 'no',
    birthMonth: '' as '' | string,
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
    birthDate: null,
  };
  newStudentPhoto: File | null = null;
  newStudentPhotoPreviewUrl: string | null = null;
  pendingCropFile: File | null = null;
  cropTarget: 'new' | 'editStudent' | null = null;

  studentColumns = signal<DataTableColumn<StudentListItem>[]>([]);
  private selectAllHeaderTpl = viewChild<TemplateRef<unknown>>('selectAllHeader');
  private rowCheckboxTpl =
    viewChild<TemplateRef<{ $implicit: StudentListItem; value: unknown }>>('rowCheckbox');
  private servantNameCellTpl =
    viewChild<TemplateRef<{ $implicit: StudentListItem; value: unknown }>>('servantNameCell');
  private lastNoteCellTpl =
    viewChild<TemplateRef<{ $implicit: StudentListItem; value: unknown }>>('lastNoteCell');
  rowActionsTpl = viewChild<TemplateRef<{ $implicit: StudentListItem }>>('rowActionsTemplate');

  constructor(
    private studentQueries: StudentQueriesService,
    private studentCommands: StudentCommandsService,
    private usersService: UsersService,
    private router: Router,
    private route: ActivatedRoute,
  ) {}

  ngAfterViewInit(): void {
    const selectAll = this.selectAllHeaderTpl();
    const rowCb = this.rowCheckboxTpl();
    const servantTpl = this.servantNameCellTpl();
    const noteTpl = this.lastNoteCellTpl();
    const actionsTpl = this.rowActionsTpl();
    const cols: DataTableColumn<StudentListItem>[] = [];
    if (selectAll && rowCb) {
      cols.push({
        key: 'id',
        header: '',
        headerTemplate: selectAll,
        template: rowCb,
      });
    }
    cols.push(
      { key: 'fullName', header: 'اسم المخدوم', sortable: true },
      { key: 'academicYear', header: 'السنة الدراسية', sortable: true },
    );
    if (servantTpl) {
      cols.push({
        key: 'servantName',
        header: 'الخادم الحالي',
        sortable: true,
        template: servantTpl,
      });
    } else {
      cols.push({ key: 'servantName', header: 'الخادم الحالي', sortable: true });
    }
    cols.push(
      { key: 'lastFollowUpDate', header: 'آخر متابعة', sortable: true, format: 'date' },
      { key: 'lastAttendanceDate', header: 'آخر حضور', sortable: true, format: 'date' },
    );
    if (noteTpl) {
      cols.push({ key: 'lastFollowUpNote', header: 'آخر ملاحظة', template: noteTpl });
    } else {
      cols.push({ key: 'lastFollowUpNote', header: 'آخر ملاحظة' });
    }
    this.studentColumns.set(cols);
  }

  ngOnInit() {
    this.route.queryParams.subscribe((params) => {
      this.filters.area = params['area'] ?? '';
      this.filters.academicYear = params['academicYear'] ?? '';
      this.filters.birthMonth = params['birthMonth'] ?? '';
      if (this.filters.area || this.filters.academicYear || this.filters.birthMonth)
        this.showFilters = true;
      this.page = 1;
      this.loadData();
    });
    this.loadOptionsAndServants();
  }

  loadData() {
    this.listRequest?.unsubscribe();this.loading=true;this.listError="";
    const scope=JSON.stringify(this.filters);if(this.scopeKey && this.scopeKey!==scope && this.selectedStudentIds.size){this.selectedStudentIds.clear();this.formMessage="تغير نطاق البحث؛ تم مسح الاختيارات.";}this.scopeKey=scope;
    const genderNum = this.filters.gender === '' ? null : Number(this.filters.gender);
    const hasServant =
      this.filters.hasServant === 'yes'
        ? true
        : this.filters.hasServant === 'no' || this.filters.servantId === '_none'
          ? false
          : null;
    const servantId =
      this.filters.servantId === '_none' || !this.filters.servantId ? null : this.filters.servantId;
    const birthMonthNum =
      this.filters.birthMonth === ''
        ? null
        : (() => {
            const n = +this.filters.birthMonth;
            return n >= 1 && n <= 12 ? n : null;
          })();
    this.listRequest=this.studentQueries
      .getPaged({
        page: this.page,
        pageSize: this.pageSize,
        search: this.filters.search.trim() || undefined,
        area: this.filters.area || undefined,
        academicYear: this.filters.academicYear || undefined,
        gender: genderNum ?? undefined,
        servantId: servantId || undefined,
        hasServant: hasServant ?? undefined,
        sortBy: this.sortBy || 'fullName',
        sortDesc: this.sortDesc,
        birthMonth: birthMonthNum ?? undefined,
      })
      .subscribe({
        next: (res) => {
          this.students = res.items;
          this.totalCount = res.totalCount;
          this.page = res.page;
          this.loading = false;
        },
        error: (e) => {this.students=[];this.loading=false;this.listError=apiError(e);},
      });
  }

  loadOptionsAndServants() {
    this.studentQueries.getDistinctAreas().subscribe({
      next: (areas) => {
        this.regionOptions = areas || [];
      },
    });
    this.studentQueries.getDistinctAcademicYears().subscribe({
      next: (years) => {
        this.academicYearOptions = years || [];
      },
    });

  }

  searchChanged(){if(this.searchTimer)clearTimeout(this.searchTimer);this.searchTimer=setTimeout(()=>this.applyFilters(),300);}
  ngOnDestroy(){this.listRequest?.unsubscribe();if(this.searchTimer)clearTimeout(this.searchTimer);this.csvReader?.abort();}
  applyFilters() {
    this.page = 1;
    this.loadData();
  }

  clearFilters() {
    this.filters = {
      search: '',
      area: '',
      academicYear: '',
      gender: '',
      servantId: '',
      hasServant: '',
      birthMonth: '',
    };
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
    else {
      this.sortBy = column;
      this.sortDesc = false;
    }
    this.page = 1;
    this.loadData();
  }

  onSortChange(event: { column: string; desc: boolean }) {
    this.sortBy = event.column;
    this.sortDesc = event.desc;
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

  async onCsvFileSelected(event: Event) {
    if (this.uploadingCsv) return;
    const input = event.target as HTMLInputElement; const file = input.files?.[0]; input.value = '';
    if (!file) return;
    this.showBulkUploadModal = false; this.uploadingCsv = true; this.uploadResult = null;
    if (file.size > 2_000_000) { this.csvError('الملف أكبر من الحد المسموح (2 ميجابايت)'); return; }
    try {
      const hash = [...new Uint8Array(await crypto.subtle.digest('SHA-256', await file.arrayBuffer()))].map(b => b.toString(16).padStart(2, '0')).join('');
      const account = localStorage.getItem('ydm.adminapp.user') || '';
      const key = `ydm.adminapp.import.${JSON.parse(account || '{}').userId || ''}.${hash}`;
      this.csvOperationId = sessionStorage.getItem(key) || crypto.randomUUID(); sessionStorage.setItem(key, this.csvOperationId);
      const reader = this.csvReader = new FileReader();
      reader.onerror = () => this.csvError('تعذر قراءة الملف. أعد اختياره.');
      reader.onabort = () => { this.uploadingCsv = false; this.csvPreview = null; };
      reader.onload = () => {
        try { this.csvPreview = this.studentCommands.previewCsv(String(reader.result || '')); this.uploadingCsv = false;this.csvReviewed=false;this.reviewCsv(); if (!this.csvPreview.length) this.csvError('الملف لا يحتوي على صفوف بيانات.'); }
        catch (e) { this.csvError(e instanceof Error ? e.message : 'ملف CSV غير صالح'); }
      };
      reader.readAsText(file, 'UTF-8');
    } catch { this.csvError('تعذر قراءة الملف. أعد اختياره.'); }
  }
  private csvError(message: string) { this.uploadingCsv = false; this.uploadResult = { created: 0, errors: [{ row: 0, message }] }; }
  cancelCsv() { this.csvReader?.abort(); this.csvPreview = null; }
  reviewCsv(){if(!this.csvPreview?.length)return;this.uploadingCsv=true;this.csvReviewError='';this.studentCommands.reviewImport(this.csvOperationId,this.csvPreview).subscribe({next:rows=>{this.uploadingCsv=false;this.csvReviewed=true;for(const row of this.csvPreview || []){const server=rows.find(r=>r.row===row.row);if(server){row.candidates=server.candidates;row.error ||= server.error || undefined;}}},error:()=>{this.uploadingCsv=false;this.csvReviewed=false;this.csvReviewError='تعذر مراجعة الأسماء المتشابهة. احتفظنا بالمعاينة؛ أعد المحاولة.';}});}
  get csvReady(){return this.csvReviewed && !!this.validCsvCount && !this.csvPreview?.some(r=>!r.error && r.candidates?.length && !r.duplicateReviewed);}
  get validCsvCount() { return this.csvPreview?.filter(r => !r.error).length || 0; }
  confirmCsv() {
    if (this.uploadingCsv || !this.csvPreview || !this.csvReady) return;
    this.uploadingCsv = true;
    this.studentCommands.importRows(this.csvOperationId, this.csvPreview).subscribe({
      next: result => { this.uploadingCsv = false; this.uploadResult = result; this.csvPreview = null; this.loadData(); },
      error: err => { this.uploadingCsv = false; this.uploadResult = { created: 0, errors: [{ row: 0, message: err.error?.message || 'تعذر إتمام الاستيراد. أعد المحاولة؛ الصفوف المحفوظة لن تتكرر.' }] }; }
    });
  }

  openAddModal() {
    this.formMessage="";
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
      birthDate: null,
    };
    if (this.newStudentPhotoPreviewUrl) {
      URL.revokeObjectURL(this.newStudentPhotoPreviewUrl);
      this.newStudentPhotoPreviewUrl = null;
    }
    this.newStudentPhoto = null;
    this.showAddModal = true;
  }

  onNewStudentPhotoFile(file: File) {
    this.pendingCropFile = file;
    this.cropTarget = 'new';
  }

  onEditStudentPhotoFile(file: File) {
    this.pendingCropFile = file;
    this.cropTarget = 'editStudent';
  }

  onCropConfirm(file: File) {
    if (this.cropTarget === 'new') {
      if (this.newStudentPhotoPreviewUrl) URL.revokeObjectURL(this.newStudentPhotoPreviewUrl);
      this.newStudentPhoto = file;
      this.newStudentPhotoPreviewUrl = URL.createObjectURL(file);
    } else if (this.cropTarget === 'editStudent') {
      if (this.editStudentPhotoPreviewUrl) URL.revokeObjectURL(this.editStudentPhotoPreviewUrl);
      if (this.editStudentExistingPhotoUrl) {
        URL.revokeObjectURL(this.editStudentExistingPhotoUrl);
        this.editStudentExistingPhotoUrl = null;
      }
      this.editStudentPhoto = file;
      this.editStudentPhotoPreviewUrl = URL.createObjectURL(file);
    }
    this.pendingCropFile = null;
    this.cropTarget = null;
  }

  onCropCancel() {
    this.pendingCropFile = null;
    this.cropTarget = null;
  }

  onCreateStudent() {
    if(this.addSaving)return;
    this.formMessage="";
    if (!this.newStudent.fullName?.trim() || !this.newStudent.phone?.trim()) {
      this.formMessage='الاسم ورقم التليفون مطلوبان';
      return;
    }
    this.addSaving = true;
    const req: CreateStudentRequest = {
      ...this.newStudent,
      servantId: this.newStudent.servantId || null,
      birthDate: this.newStudent.birthDate || null,
    };
    this.studentCommands.create(req).subscribe({
      next: (id) => {
        if (this.newStudentPhoto) {
          this.studentCommands.uploadPhoto(id, this.newStudentPhoto).subscribe({
            next: () => this.finishCreateStudent(),
            error: () => {this.photoRetry={id,file:this.newStudentPhoto!};this.formMessage="تم حفظ المخدوم، لكن تعذر رفع الصورة. أعد رفع الصورة فقط.";this.finishCreateStudent();},
          });
        } else {
          this.finishCreateStudent();
        }
      },
      error: (err) => {
        this.addSaving = false;
        this.formMessage=apiError(err);
      },
    });
  }

  private finishCreateStudent() {
    this.showAddModal = false;
    this.addSaving = false;
    if (this.newStudentPhotoPreviewUrl) {
      URL.revokeObjectURL(this.newStudentPhotoPreviewUrl);
      this.newStudentPhotoPreviewUrl = null;
    }
    this.newStudentPhoto = null;
    this.loadData();
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

  clearSelection() { if(this.bulkAssignSaving)return;this.selectedStudentIds=new Set<string>();this.formMessage='تم مسح الاختيارات عبر الصفحات.'; }

  selectAllFiltered() {
    const all = this.students.every((s: any) => this.selectedStudentIds.has(s.id));
    if (all) this.students.forEach(s => this.selectedStudentIds.delete(s.id));
    else this.students.forEach((s: any) => this.selectedStudentIds.add(s.id));
    this.selectedStudentIds = new Set(this.selectedStudentIds);
  }

  get allFilteredSelected(): boolean {
    return (
      this.students.length > 0 && this.students.every((s: any) => this.selectedStudentIds.has(s.id))
    );
  }

  openBulkAssignModal() {
    if (this.selectedStudentIds.size === 0) return;
    this.bulkTargetServantId = '';
    this.showBulkAssignModal = true;
  }

  onBulkAssign() {
    if (this.bulkAssignSaving || !this.bulkTargetServantId || this.selectedStudentIds.size === 0) return;
    this.bulkAssignSaving = true;
    const ids = Array.from(this.selectedStudentIds);
    this.studentCommands.bulkAssign(ids, this.bulkTargetServantId).subscribe({
      next: result => {
        this.bulkAssignSaving = false;
        this.selectedStudentIds = new Set(result.outcomes.filter(o => !o.success).map(o => o.studentId));
        this.uploadResult = { created: result.outcomes.filter(o => o.success).length, errors: result.outcomes.filter(o => !o.success).map((o, i) => ({ row: i + 1, message: o.error || 'تعذر التخصيص؛ الاختيار محفوظ لإعادة المحاولة' })) };
        if (!this.selectedStudentIds.size) this.showBulkAssignModal = false;
        this.loadData();
      },
      error: () => { this.bulkAssignSaving = false; this.uploadResult = { created: 0, errors: [{ row: 0, message: 'تعذر التخصيص. الاختيارات محفوظة لإعادة المحاولة.' }] }; }
    });
  }
  get pageSelectionIndeterminate() { return this.students.some(s => this.selectedStudentIds.has(s.id)) && !this.allFilteredSelected; }


  openEdit(s: any) {
    this.formMessage="";
    this.studentQueries.getById(s.id).subscribe({
      next: (st) => {
        this.editingStudent = st;
        this.editForm = {
          fullName: st.fullName || '',
          phone: st.phone || '',
          secondaryPhone:st.secondaryPhone ?? null,birthMonth:st.birthMonth ?? null,notes:st.notes ?? null,
          address: st.address ?? null,
          area: st.area ?? null,
          college: st.college ?? null,
          academicYear: st.academicYear ?? null,
          confessionFather: st.confessionFather ?? null,
          gender: st.gender ?? 0,
          servantId: st.servantId ?? null,
          birthDate: dateOnly(st.birthDate) || null,
        };
        this.editStudentPhoto = null;
        if (this.editStudentPhotoPreviewUrl) {
          URL.revokeObjectURL(this.editStudentPhotoPreviewUrl);
          this.editStudentPhotoPreviewUrl = null;
        }
        if (this.editStudentExistingPhotoUrl) {
          URL.revokeObjectURL(this.editStudentExistingPhotoUrl);
          this.editStudentExistingPhotoUrl = null;
        }
        if (st.photoPath) {
          this.studentQueries.getPhotoBlobUrl(s.id).subscribe({
            next: (url) => {
              this.editStudentExistingPhotoUrl = url ?? null;
            },
            error: () => {},
          });
        }
        this.showEditModal = true;
      },
      error: (err) => {this.formMessage=apiError(err);},
    });
  }

  closeEdit() {
    this.showEditModal = false;
    this.editingStudent = null;
    this.editStudentPhoto = null;
    if (this.editStudentPhotoPreviewUrl) {
      URL.revokeObjectURL(this.editStudentPhotoPreviewUrl);
      this.editStudentPhotoPreviewUrl = null;
    }
    if (this.editStudentExistingPhotoUrl) {
      URL.revokeObjectURL(this.editStudentExistingPhotoUrl);
      this.editStudentExistingPhotoUrl = null;
    }
  }

  onUpdateStudent() {
    if (!this.editingStudent || this.editSaving || !this.editForm.fullName.trim() || !this.editForm.phone.trim()) return;
    this.formMessage="";
    this.editSaving = true;
    const req: UpdateStudentRequest = {
      id: this.editingStudent.id,
      fullName: this.editForm.fullName.trim(),
      phone: this.editForm.phone.trim(),
      secondaryPhone:this.editForm.secondaryPhone,notes:this.editForm.notes,birthMonth:this.editForm.birthMonth,
      address: this.editForm.address ?? null,
      area: this.editForm.area ?? null,
      college: this.editForm.college ?? null,
      academicYear: this.editForm.academicYear ?? null,
      confessionFather: this.editForm.confessionFather ?? null,
      gender: this.editForm.gender ?? 0,
      servantId: this.editForm.servantId ?? null,
      birthDate: this.editForm.birthDate || null,
    };
    this.studentCommands.update(req,this.editingStudent?.version).subscribe({
      next: () => {
        if (this.editStudentPhoto) {
          this.studentCommands
            .uploadPhoto(this.editingStudent.id, this.editStudentPhoto)
            .subscribe({
              next: (res) => {
                this.editingStudent.photoPath = res.photoPath;
                this.editSaving = false;
                this.closeEdit();
                this.loadData();
              },
              error: () => {
                this.photoRetry={id:this.editingStudent.id,file:this.editStudentPhoto!};this.formMessage="تم حفظ التعديلات، لكن تعذر رفع الصورة. أعد رفع الصورة فقط.";
                this.editSaving = false;
                this.closeEdit();
                this.loadData();
              },
            });
        } else {
          this.editSaving = false;
          this.closeEdit();
          this.loadData();
        }
      },
      error: (err: any) => {
        this.editSaving = false;
        this.formMessage=apiError(err);
      },
    });
  }

  archiveTarget:any=null;archiveReason='';archiveBusy=false;archiveError='';
  deleteStudent(s:any){this.archiveTarget=s;this.archiveReason='';this.archiveError='';}
  archiveStudent(){if(!this.archiveTarget || this.archiveBusy || !this.archiveReason.trim())return;this.archiveBusy=true;this.archiveError='';this.studentCommands.delete(this.archiveTarget.id,this.archiveReason,this.archiveTarget.version).subscribe({next:()=>{this.archiveBusy=false;this.selectedStudentIds.delete(this.archiveTarget.id);this.selectedStudentIds=new Set(this.selectedStudentIds);this.archiveTarget=null;this.formMessage='تمت أرشفة المخدوم وإلغاء العمل المفتوح. يمكن استعادته دون إسناد.';this.loadData();},error:e=>{this.archiveBusy=false;this.archiveError=apiError(e);}});}

  onAssign() {
    if(!this.targetServantId || this.assignSaving || !this.selectedStudent)return;
    this.assignSaving=true;this.formMessage="";
    const request=this.targetServantId==='_none' ? this.studentCommands.unassign(this.selectedStudent.id) : this.studentCommands.assignToServant(this.selectedStudent.id,this.targetServantId);
    request.subscribe({next:()=>{this.assignSaving=false;this.showAssignModal=false;this.formMessage="تم حفظ الإسناد.";this.loadData();},error:e=>{this.assignSaving=false;this.formMessage=apiError(e);}});
  }
  safePicker(input:HTMLInputElement){try{input.showPicker?.();}catch{input.focus();}}
  retryPhoto(){if(!this.photoRetry || this.photoRetryBusy)return;const retry=this.photoRetry;this.photoRetryBusy=true;this.studentCommands.uploadPhoto(retry.id,retry.file).subscribe({next:()=>{this.photoRetryBusy=false;this.photoRetry=null;this.formMessage="تم رفع الصورة بنجاح.";},error:()=>{this.photoRetryBusy=false;this.formMessage="تعذر رفع الصورة؛ بيانات المخدوم محفوظة. يمكنك المحاولة مجددًا.";}});}

}
