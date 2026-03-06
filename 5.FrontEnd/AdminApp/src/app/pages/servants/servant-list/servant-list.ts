import { AfterViewInit, Component, OnInit, TemplateRef, signal, viewChild } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import {
  CardComponent,
  ContentHeaderComponent,
  FiltersBarComponent,
  FormFieldComponent,
  LoaderComponent,
  ModalComponent,
  PhotoCropModalComponent,
  ProfilePhotoInputComponent,
} from '../../../components/common/common';
import { ReportsService } from '../../../services/reports.service';
import { UsersService } from '../../../services/users.service';
import type { DataTableColumn } from '../../../shared/components/data-table/data-table-column';
import { DataTableComponent } from '../../../shared/components/data-table/data-table.component';
import { ServantWithStats } from '../../../shared/models/report.model';
import { DateFormatPipe } from '../../../shared/pipes/date-format.pipe';

@Component({
  selector: 'app-servant-list',
  standalone: true,
  imports: [
    DateFormatPipe,
    FormsModule,
    ContentHeaderComponent,
    FiltersBarComponent,
    FormFieldComponent,
    CardComponent,
    LoaderComponent,
    ModalComponent,
    PhotoCropModalComponent,
    ProfilePhotoInputComponent,
    DataTableComponent,
  ],
  templateUrl: './servant-list.html',
  styleUrls: ['./servant-list.css'],
})
export class ServantListComponent implements OnInit, AfterViewInit {
  servants: ServantWithStats[] = [];
  totalCount = 0;
  page = 1;
  pageSize = 10;
  pageSizeOptions = [5, 10, 20, 50];
  loading = true;
  showModal = false;
  showEditModal = false;
  showFilters = false;
  filters = {
    search: '',
    role: '' as '' | 'Servant' | 'Manager' | 'Priest' | 'Admin',
    status: '' as '' | 'active' | 'inactive',
  };
  sortBy: string | null = 'fullName';
  sortDesc = false;
  priestAlreadyExists = false;
  priestOptionDisabledInEdit = false;

  editingServant: ServantWithStats | null = null;
  editForm = { fullName: '', phone: '', isActive: true, role: 'Servant' as string };

  newServant = {
    userName: '',
    fullName: '',
    email: '',
    phone: '',
    password: '',
    role: 'Servant',
  };
  newServantPhoto: File | null = null;
  newServantPhotoPreviewUrl: string | null = null;
  editServantPhoto: File | null = null;
  editServantPhotoPreviewUrl: string | null = null;
  editServantExistingPhotoUrl: string | null = null;
  creatingServant = false;
  editSaving = false;
  pendingCropFile: File | null = null;
  cropTarget: 'new' | 'edit' | null = null;

  servantColumns = signal<DataTableColumn<ServantWithStats>[]>([]);
  private roleCellTpl =
    viewChild<TemplateRef<{ $implicit: ServantWithStats; value: unknown }>>('roleCell');
  private statusCellTpl =
    viewChild<TemplateRef<{ $implicit: ServantWithStats; value: unknown }>>('statusCell');
  private lastCallDateTpl =
    viewChild<TemplateRef<{ $implicit: ServantWithStats; value: unknown }>>('lastCallDateCell');
  private lastVisitDateTpl =
    viewChild<TemplateRef<{ $implicit: ServantWithStats; value: unknown }>>('lastVisitDateCell');
  rowActionsTpl = viewChild<TemplateRef<{ $implicit: ServantWithStats }>>('rowActionsTemplate');

  constructor(
    private usersService: UsersService,
    private router: Router,
    private reportsService: ReportsService,
  ) {}

  viewServant(s: ServantWithStats) {
    this.router.navigate(['/dashboard/servants', s.id]);
  }

  ngOnInit() {
    this.loadServants();
  }

  ngAfterViewInit() {
    const roleTpl = this.roleCellTpl();
    const statusTpl = this.statusCellTpl();
    const lastCallTpl = this.lastCallDateTpl();
    const lastVisitTpl = this.lastVisitDateTpl();
    const cols: DataTableColumn<ServantWithStats>[] = [
      { key: 'fullName', header: 'الاسم', sortable: true },
      { key: 'userName', header: 'اسم المستخدم', sortable: true },
      {
        key: 'role',
        header: 'الدور',
        sortable: true,
        ...(roleTpl && { template: roleTpl }),
      },
      { key: 'assignedStudentsCount', header: 'عدد المخدومين', sortable: true },
      {
        key: 'lastCallDate',
        header: 'آخر مكالمة',
        sortable: true,
        ...(lastCallTpl ? { template: lastCallTpl } : { format: 'date' }),
      },
      {
        key: 'lastVisitDate',
        header: 'آخر زيارة',
        sortable: true,
        ...(lastVisitTpl ? { template: lastVisitTpl } : { format: 'date' }),
      },
      {
        key: 'isActive',
        header: 'الحالة',
        sortable: true,
        ...(statusTpl && { template: statusTpl }),
      },
    ];
    this.servantColumns.set(cols);
  }

  onSortChange(event: { column: string; desc: boolean }) {
    this.sortBy = event.column;
    this.sortDesc = event.desc;
    this.page = 1;
    this.loadServants();
  }

  loadServants() {
    const isActive =
      this.filters.status === 'active'
        ? true
        : this.filters.status === 'inactive'
          ? false
          : undefined;
    this.loading = true;
    this.reportsService
      .getServantsPagedWithStats({
        page: this.page,
        pageSize: this.pageSize,
        search: this.filters.search.trim() || undefined,
        role: this.filters.role || undefined,
        isActive: isActive as boolean | undefined,
        sortBy: this.sortBy ?? undefined,
        sortDesc: this.sortDesc,
      })
      .subscribe({
        next: (res) => {
          this.servants = res.items;
          this.totalCount = res.totalCount;
          this.page = res.page;
          this.loading = false;
        },
        error: () => (this.loading = false),
      });
  }

  roleLabel(role: string): string {
    const map: Record<string, string> = {
      Servant: 'خادم',
      Manager: 'مسئول',
      Secretary: 'سكرتارية',
      Priest: 'الاب الكاهن المسئول',
    };
    return map[role] ?? role;
  }

  openAddModal() {
    this.usersService.getPaged({ page: 1, pageSize: 1, role: 'Priest' }).subscribe({
      next: (r) => {
        this.priestAlreadyExists = r.totalCount > 0;
        this.showModal = true;
      },
    });
  }

  applyFilters() {
    this.page = 1;
    this.loadServants();
  }

  clearFilters() {
    this.filters = { search: '', role: '' as any, status: '' as any };
    this.page = 1;
    this.loadServants();
  }

  get totalPages(): number {
    return Math.max(1, Math.ceil(this.totalCount / this.pageSize));
  }

  goToPage(p: number) {
    if (p < 1 || p > this.totalPages) return;
    this.page = p;
    this.loadServants();
  }

  onPageSizeChange() {
    this.page = 1;
    this.loadServants();
  }

  setPageSize(n: number) {
    this.pageSize = Number(n);
    this.onPageSizeChange();
  }

  toggleServantStatus(servant: ServantWithStats) {
    this.usersService.toggleStatus(servant.id).subscribe(() => {
      servant.isActive = !servant.isActive;
    });
  }

  onNewServantPhotoFile(file: File) {
    this.pendingCropFile = file;
    this.cropTarget = 'new';
  }

  onEditServantPhotoFile(file: File) {
    this.pendingCropFile = file;
    this.cropTarget = 'edit';
  }

  onCropConfirm(file: File) {
    if (this.cropTarget === 'new') {
      if (this.newServantPhotoPreviewUrl) URL.revokeObjectURL(this.newServantPhotoPreviewUrl);
      this.newServantPhoto = file;
      this.newServantPhotoPreviewUrl = URL.createObjectURL(file);
    } else if (this.cropTarget === 'edit') {
      if (this.editServantPhotoPreviewUrl) URL.revokeObjectURL(this.editServantPhotoPreviewUrl);
      if (this.editServantExistingPhotoUrl) {
        URL.revokeObjectURL(this.editServantExistingPhotoUrl);
        this.editServantExistingPhotoUrl = null;
      }
      this.editServantPhoto = file;
      this.editServantPhotoPreviewUrl = URL.createObjectURL(file);
    }
    this.closeCropModal();
  }

  onCropCancel() {
    this.closeCropModal();
  }

  private closeCropModal() {
    this.pendingCropFile = null;
    this.cropTarget = null;
  }

  onCreateServant() {
    this.creatingServant = true;
    this.usersService.create(this.newServant).subscribe({
      next: (id) => {
        if (this.newServantPhoto) {
          this.usersService.uploadPhoto(id, this.newServantPhoto!).subscribe({
            next: () => {
              this.finishCreateServant();
            },
            error: () => {
              this.finishCreateServant();
            },
          });
        } else {
          this.finishCreateServant();
        }
      },
      error: (err: any) => {
        this.creatingServant = false;
        alert('خطأ في إنشاء الخادم: ' + (err.error?.message ?? err.error ?? err.message));
      },
    });
  }

  private finishCreateServant() {
    this.creatingServant = false;
    this.showModal = false;
    this.loadServants();
    this.resetForm();
  }

  resetForm() {
    this.newServant = {
      userName: '',
      fullName: '',
      email: '',
      phone: '',
      password: '',
      role: 'Servant',
    };
    if (this.newServantPhotoPreviewUrl) {
      URL.revokeObjectURL(this.newServantPhotoPreviewUrl);
      this.newServantPhotoPreviewUrl = null;
    }
    this.newServantPhoto = null;
  }

  openEdit(s: ServantWithStats) {
    this.editingServant = s;
    this.editForm = {
      fullName: s.fullName || '',
      phone: s.phone || '',
      isActive: s.isActive,
      role: s.role ?? 'Servant',
    };
    this.editServantPhoto = null;
    if (this.editServantPhotoPreviewUrl) {
      URL.revokeObjectURL(this.editServantPhotoPreviewUrl);
      this.editServantPhotoPreviewUrl = null;
    }
    if (this.editServantExistingPhotoUrl) {
      URL.revokeObjectURL(this.editServantExistingPhotoUrl);
      this.editServantExistingPhotoUrl = null;
    }
    if (s.photoPath) {
      this.usersService.getPhotoBlobUrl(s.id).subscribe({
        next: (url) => {
          this.editServantExistingPhotoUrl = url ?? null;
        },
        error: () => {},
      });
    }
    this.usersService.getPaged({ page: 1, pageSize: 10, role: 'Priest' }).subscribe({
      next: (r) => {
        this.priestOptionDisabledInEdit = r.items.some((u: { id: string }) => u.id !== s.id);
      },
    });
    this.showEditModal = true;
  }

  closeEdit() {
    this.showEditModal = false;
    this.editingServant = null;
    this.editServantPhoto = null;
    if (this.editServantPhotoPreviewUrl) {
      URL.revokeObjectURL(this.editServantPhotoPreviewUrl);
      this.editServantPhotoPreviewUrl = null;
    }
    if (this.editServantExistingPhotoUrl) {
      URL.revokeObjectURL(this.editServantExistingPhotoUrl);
      this.editServantExistingPhotoUrl = null;
    }
  }

  onUpdateServant() {
    if (!this.editingServant) return;
    this.editSaving = true;
    this.usersService.update(this.editingServant.id, this.editForm).subscribe({
      next: () => {
        this.editingServant!.fullName = this.editForm.fullName;
        this.editingServant!.phone = this.editForm.phone;
        this.editingServant!.isActive = this.editForm.isActive;
        this.editingServant!.role = this.editForm.role;
        if (this.editServantPhoto) {
          this.usersService.uploadPhoto(this.editingServant!.id, this.editServantPhoto).subscribe({
            next: (res) => {
              this.editingServant!.photoPath = res.photoPath;
              this.editSaving = false;
              this.closeEdit();
            },
            error: () => {
              this.editSaving = false;
              this.closeEdit();
            },
          });
        } else {
          this.editSaving = false;
          this.closeEdit();
        }
      },
      error: (err: any) => {
        this.editSaving = false;
        alert('خطأ في التحديث: ' + (err.error || err.message));
      },
    });
  }

  deleteServant(s: ServantWithStats) {
    if (!confirm(`حذف الخادم «${s.fullName}»؟ لا يمكن التراجع.`)) return;
    this.usersService.delete(s.id).subscribe({
      next: () => this.loadServants(),
      error: (err: any) => alert('خطأ في الحذف: ' + (err.error?.message || err.message)),
    });
  }
}
