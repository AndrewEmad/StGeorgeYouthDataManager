import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { forkJoin } from 'rxjs';
import { StudentQueriesService } from '../../services/student-queries.service';
import { FollowUpService } from '../../services/follow-up.service';
import { StudentCommandsService } from '../../services/student-commands.service';
import { RemovalRequestService } from '../../services/removal-request.service';
import { AuthService } from '../../services/auth.service';

const CALL_STATUS_OPTIONS = [
  { value: 0, label: 'لم يجب' },
  { value: 1, label: 'أجاب' },
  { value: 2, label: 'مشغول' },
  { value: 3, label: 'مغلق' },
  { value: 4, label: 'رقم خاطئ' }
];

const VISIT_OUTCOME_OPTIONS = [
  { value: 0, label: 'ناجحة' },
  { value: 1, label: 'غير موجود' },
  { value: 2, label: 'رفض الزيارة' },
  { value: 3, label: 'مؤجلة' }
];

@Component({
  selector: 'app-student-detail',
  standalone: true,
  imports: [CommonModule, RouterLink, FormsModule],
  templateUrl: './student-detail.html',
  styleUrls: ['./student-detail.css']
})
export class StudentDetailComponent implements OnInit {
  student: any = null;
  callHistory: any[] = [];
  visitHistory: any[] = [];
  loading = true;
  editing = false;
  editForm: any = {};
  showAddCall = false;
  showAddVisit = false;
  removalRequest: any = null;

  callDate = '';
  callStatus = 0;
  callNotes = '';
  callNextDate = '';
  callSaving = false;
  callError = '';

  visitDate = '';
  visitOutcome = 0;
  visitNotes = '';
  visitNextDate = '';
  visitSaving = false;
  visitError = '';

  saveEditLoading = false;
  editError = '';
  requestRemovalLoading = false;
  removalError = '';

  readonly callStatusOptions = CALL_STATUS_OPTIONS;
  readonly visitOutcomeOptions = VISIT_OUTCOME_OPTIONS;

  constructor(
    private route: ActivatedRoute,
    private studentQueriesService: StudentQueriesService,
    private followUpService: FollowUpService,
    private studentCommandsService: StudentCommandsService,
    private removalRequestService: RemovalRequestService,
    private authService: AuthService
  ) {}

  ngOnInit() {
    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.loadStudent(id);
    }
  }

  loadStudent(id: string) {
    this.loading = true;
    this.studentQueriesService.getById(id).subscribe({
      next: (data: any) => {
        this.student = data;
        this.editForm = {
          id: data.id,
          fullName: data.fullName,
          phone: data.phone,
          address: data.address || '',
          area: data.area || '',
          college: data.college || '',
          academicYear: data.academicYear || '',
          confessionFather: data.confessionFather || '',
          gender: data.gender ?? 0,
          birthDate: data.birthDate ? data.birthDate.split('T')[0] : ''
        };
        this.loadHistory(id);
        this.removalRequestService.getPendingForStudent(id).subscribe({
          next: (req) => (this.removalRequest = req),
          error: () => {}
        });
      },
      error: () => {
        this.loading = false;
      }
    });
  }

  loadHistory(studentId: string) {
    forkJoin({
      calls: this.followUpService.getCallHistory(studentId),
      visits: this.followUpService.getVisitHistory(studentId)
    }).subscribe({
      next: (r) => {
        const calls = Array.isArray(r.calls) ? r.calls : [];
        const visits = Array.isArray(r.visits) ? r.visits : [];
        this.callHistory = [...calls].sort((a, b) => new Date((b as any).callDate).getTime() - new Date((a as any).callDate).getTime());
        this.visitHistory = [...visits].sort((a, b) => new Date((b as any).visitDate).getTime() - new Date((a as any).visitDate).getTime());
        this.loading = false;
      },
      error: () => {
        this.callHistory = [];
        this.visitHistory = [];
        this.loading = false;
      }
    });
  }

  getCallStatusLabel(status: number): string {
    return this.callStatusOptions.find(o => o.value === status)?.label ?? String(status);
  }

  getVisitOutcomeLabel(outcome: number): string {
    return this.visitOutcomeOptions.find(o => o.value === outcome)?.label ?? String(outcome);
  }

  reloadHistory() {
    if (this.student?.id) this.loadHistory(this.student.id);
  }

  openAddCall() {
    this.callDate = new Date().toISOString().slice(0, 16);
    this.callStatus = 0;
    this.callNotes = '';
    this.callNextDate = '';
    this.callError = '';
    this.showAddCall = true;
  }

  closeAddCall() {
    this.showAddCall = false;
  }

  submitCall() {
    if (!this.student?.id) return;
    this.callSaving = true;
    this.callError = '';
    const payload = {
      studentId: this.student.id,
      callDate: new Date(this.callDate).toISOString(),
      callStatus: Number(this.callStatus),
      notes: this.callNotes || '',
      nextFollowUpDate: this.callNextDate ? new Date(this.callNextDate).toISOString() : null
    };
    this.followUpService.registerCall(payload).subscribe({
      next: () => {
        this.closeAddCall();
        this.reloadHistory();
        this.callSaving = false;
      },
      error: (err) => {
        this.callError = err.error?.message || 'فشل تسجيل المكالمة';
        this.callSaving = false;
      }
    });
  }

  openAddVisit() {
    this.visitDate = new Date().toISOString().slice(0, 16);
    this.visitOutcome = 0;
    this.visitNotes = '';
    this.visitNextDate = '';
    this.visitError = '';
    this.showAddVisit = true;
  }

  closeAddVisit() {
    this.showAddVisit = false;
  }

  submitVisit() {
    if (!this.student?.id) return;
    this.visitSaving = true;
    this.visitError = '';
    const payload = {
      studentId: this.student.id,
      visitDate: new Date(this.visitDate).toISOString(),
      visitOutcome: Number(this.visitOutcome),
      notes: this.visitNotes || '',
      nextVisitDate: this.visitNextDate ? new Date(this.visitNextDate).toISOString() : null
    };
    this.followUpService.registerVisit(payload).subscribe({
      next: () => {
        this.closeAddVisit();
        this.reloadHistory();
        this.visitSaving = false;
      },
      error: (err) => {
        this.visitError = err.error?.message || 'فشل تسجيل الزيارة';
        this.visitSaving = false;
      }
    });
  }

  startEdit() {
    this.editForm = {
      id: this.student.id,
      fullName: this.student.fullName,
      phone: this.student.phone,
      address: this.student.address || '',
      area: this.student.area || '',
      college: this.student.college || '',
      academicYear: this.student.academicYear || '',
      confessionFather: this.student.confessionFather || '',
      gender: this.student.gender ?? 0,
      birthDate: this.student.birthDate ? String(this.student.birthDate).split('T')[0] : '',
      servantId: this.student.servantId
    };
    this.editing = true;
    this.editError = '';
  }

  cancelEdit() {
    this.editing = false;
  }

  saveEdit() {
    this.saveEditLoading = true;
    this.editError = '';
    const req = {
      ...this.editForm,
      birthDate: this.editForm.birthDate || null
    };
    this.studentCommandsService.update(this.student.id, req).subscribe({
      next: () => {
        this.student = { ...this.student, ...this.editForm };
        this.editing = false;
        this.saveEditLoading = false;
      },
      error: (err) => {
        this.editError = err.error?.message || 'فشل التحديث';
        this.saveEditLoading = false;
      }
    });
  }

  requestRemoval() {
    if (!this.student?.id || this.removalRequest) return;
    this.requestRemovalLoading = true;
    this.removalError = '';
    this.removalRequestService.create(this.student.id).subscribe({
      next: () => {
        this.removalRequest = { status: 'Pending' };
        this.requestRemovalLoading = false;
      },
      error: (err) => {
        this.removalError = err.error?.message || 'فشل تقديم الطلب';
        this.requestRemovalLoading = false;
      }
    });
  }

  canEdit(): boolean {
    const uid = this.authService.currentUser()?.userId;
    return !!uid && this.student?.servantId === uid;
  }
}
