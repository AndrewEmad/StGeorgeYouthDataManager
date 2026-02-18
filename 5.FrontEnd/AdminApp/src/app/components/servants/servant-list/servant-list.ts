import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { UsersService, User } from '../../../services/users.service';
import { ReportsService } from '../../../services/reports.service';

@Component({
  selector: 'app-servant-list',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './servant-list.html',
  styleUrls: ['./servant-list.css']
})
export class ServantListComponent implements OnInit {
  servants: User[] = [];
  totalCount = 0;
  page = 1;
  pageSize = 10;
  pageSizeOptions = [5, 10, 20, 50];
  loading = true;
  showModal = false;
  showEditModal = false;
  showFilters = false;
  filters = { search: '', role: '' as '' | 'Servant' | 'Manager' | 'Priest', status: '' as '' | 'active' | 'inactive' };
  servantStatsMap: Record<string, { assignedStudentsCount: number; lastCallDate: string | null; lastVisitDate: string | null }> = {};
  priestAlreadyExists = false;

  editingServant: User | null = null;
  editForm = { fullName: '', phone: '', isActive: true };

  newServant = {
    userName: '',
    fullName: '',
    email: '',
    phone: '',
    password: '',
    role: 'Servant'
  };

  constructor(private usersService: UsersService, private router: Router, private reportsService: ReportsService) {}

  viewServant(s: User) {
    this.router.navigate(['/dashboard/servants', s.id]);
  }

  ngOnInit() {
    this.loadServants();
  }

  loadServants() {
    const isActive = this.filters.status === 'active' ? true : this.filters.status === 'inactive' ? false : undefined;
    this.usersService.getPaged({
      page: this.page,
      pageSize: this.pageSize,
      search: this.filters.search.trim() || undefined,
      role: this.filters.role || undefined,
      isActive: isActive as boolean | undefined
    }).subscribe({
      next: (res) => {
        this.servants = res.items;
        this.totalCount = res.totalCount;
        this.page = res.page;
        const ids = res.items.map((u: User) => u.id);
        if (ids.length) {
          this.reportsService.getServantStats(ids).subscribe({
            next: (stats) => {
              this.servantStatsMap = {};
              stats.forEach((st: any) => { this.servantStatsMap[st.servantId] = { assignedStudentsCount: st.assignedStudentsCount, lastCallDate: st.lastCallDate ?? null, lastVisitDate: st.lastVisitDate ?? null }; });
              this.loading = false;
            },
            error: () => this.loading = false
          });
        } else {
          this.servantStatsMap = {};
          this.loading = false;
        }
      },
      error: () => this.loading = false
    });
  }

  roleLabel(role: string): string {
    const map: Record<string, string> = { Servant: 'خادم', Manager: 'أمين خدمة', Priest: 'الاب الكاهن المسئول' };
    return map[role] ?? role;
  }

  openAddModal() {
    this.usersService.getPaged({ page: 1, pageSize: 1, role: 'Priest' }).subscribe({
      next: (r) => { this.priestAlreadyExists = r.totalCount > 0; this.showModal = true; }
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

  toggleServantStatus(servant: User) {
    this.usersService.toggleStatus(servant.id).subscribe(() => {
      servant.isActive = !servant.isActive;
    });
  }

  onCreateServant() {
    this.usersService.create(this.newServant).subscribe({
      next: () => {
        this.showModal = false;
        this.loadServants();
        this.resetForm();
      },
      error: (err: any) => alert('خطأ في إنشاء الخادم: ' + (err.error?.message ?? err.error ?? err.message))
    });
  }

  resetForm() {
    this.newServant = {
      userName: '',
      fullName: '',
      email: '',
      phone: '',
      password: '',
      role: 'Servant'
    };
  }

  openEdit(s: User) {
    this.editingServant = s;
    this.editForm = { fullName: s.fullName || '', phone: s.phone || '', isActive: s.isActive };
    this.showEditModal = true;
  }

  closeEdit() {
    this.showEditModal = false;
    this.editingServant = null;
  }

  onUpdateServant() {
    if (!this.editingServant) return;
    this.usersService.update(this.editingServant.id, this.editForm).subscribe({
      next: () => {
        this.editingServant!.fullName = this.editForm.fullName;
        this.editingServant!.phone = this.editForm.phone;
        this.editingServant!.isActive = this.editForm.isActive;
        this.closeEdit();
      },
      error: (err: any) => alert('خطأ في التحديث: ' + (err.error || err.message))
    });
  }

  deleteServant(s: User) {
    if (!confirm(`حذف الخادم «${s.fullName}»؟ لا يمكن التراجع.`)) return;
    this.usersService.delete(s.id).subscribe({
      next: () => this.loadServants(),
      error: (err: any) => alert('خطأ في الحذف: ' + (err.error?.message || err.message))
    });
  }
}
