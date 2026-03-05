import { ChangeDetectionStrategy, Component, computed, DestroyRef, inject, signal } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { forkJoin } from 'rxjs';
import { StudentQueriesService } from '../../services/student-queries.service';
import { FollowUpService } from '../../services/follow-up.service';
import { StudentCommandsService } from '../../services/student-commands.service';
import { RemovalRequestService } from '../../services/removal-request.service';
import { AuthService } from '../../core/services/auth.service';
import { DatePipe } from '@angular/common';
import { ProfilePhotoInputComponent } from '../../shared/components';
import { CallStatusLabelPipe, VisitOutcomeLabelPipe } from '../../shared/pipes';
import type { Student } from '../../shared/models/student.model';
import type { CallRecord, VisitRecord } from '../../shared/models/follow-up.model';
import { CALL_STATUS_OPTIONS, VISIT_OUTCOME_OPTIONS } from '../../shared/models/follow-up.model';
import { fromIsoToDateOnly } from '../../shared/utils/date.utils';

@Component({
  selector: 'app-student-detail',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [DatePipe, ProfilePhotoInputComponent, CallStatusLabelPipe, VisitOutcomeLabelPipe],
  templateUrl: './student-detail.html',
  styleUrls: ['./student-detail.css'],
})
export class StudentDetailComponent {
  private readonly route = inject(ActivatedRoute);
  private readonly studentQueriesService = inject(StudentQueriesService);
  private readonly followUpService = inject(FollowUpService);
  private readonly studentCommandsService = inject(StudentCommandsService);
  private readonly removalRequestService = inject(RemovalRequestService);
  private readonly authService = inject(AuthService);
  private readonly destroyRef = inject(DestroyRef);

  readonly student = signal<Student | null>(null);
  readonly studentPhotoUrl = signal<string | null>(null);
  readonly uploadingPhoto = signal(false);
  readonly photoError = signal('');
  readonly callHistory = signal<CallRecord[]>([]);
  readonly visitHistory = signal<VisitRecord[]>([]);
  readonly loading = signal(true);
  readonly editing = signal(false);
  readonly editForm = signal<Record<string, unknown>>({});
  readonly showAddCall = signal(false);
  readonly showAddVisit = signal(false);
  readonly showEditVisit = signal(false);
  readonly visitToEdit = signal<VisitRecord | null>(null);
  readonly showRemovalChoiceModal = signal(false);
  readonly removalRequest = signal<{ status: string } | null>(null);

  readonly callDate = signal('');
  readonly callStatus = signal(0);
  readonly callNotes = signal('');
  readonly callNextDate = signal('');
  readonly callSaving = signal(false);
  readonly callError = signal('');

  readonly visitDate = signal('');
  readonly visitOutcome = signal(0);
  readonly visitNotes = signal('');
  readonly visitNextDate = signal('');
  readonly visitSaving = signal(false);
  readonly visitError = signal('');
  readonly servantList = signal<{ id: string; fullName: string }[]>([]);
  readonly selectedParticipantIds = signal<string[]>([]);
  readonly participantSearchQuery = signal('');

  readonly saveEditLoading = signal(false);
  readonly editError = signal('');
  readonly requestRemovalLoading = signal(false);
  readonly removalError = signal('');

  readonly callStatusOptions = CALL_STATUS_OPTIONS;
  readonly visitOutcomeOptions = VISIT_OUTCOME_OPTIONS;

  readonly filteredServantList = computed(() => {
    const q = (this.participantSearchQuery() || '').trim().toLowerCase();
    const list = this.servantList();
    if (!q) return list;
    return list.filter((s) => (s.fullName || '').toLowerCase().includes(q));
  });

  readonly canEdit = computed(() => {
    const uid = this.authService.currentUser()?.userId;
    return !!uid && this.student()?.servantId === uid;
  });

  constructor() {
    const id = this.route.snapshot.paramMap.get('id');
    if (id) this.loadStudent(id);
    this.followUpService
      .getServants()
      .pipe(takeUntilDestroyed())
      .subscribe({
        next: (list) => this.servantList.set(list ?? []),
        error: () => this.servantList.set([]),
      });
  }

  onStudentPhotoSelected(file: File): void {
    const s = this.student();
    if (!s?.id) return;
    this.photoError.set('');
    this.uploadingPhoto.set(true);
    this.studentCommandsService
      .uploadPhoto(s.id, file)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (res) => {
          this.student.update((prev) => (prev ? { ...prev, photoPath: res.photoPath } : null));
          const url = this.studentPhotoUrl();
          if (url) URL.revokeObjectURL(url);
          this.studentQueriesService
            .getPhotoBlobUrl(s.id)
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe((url) => this.studentPhotoUrl.set(url));
          this.uploadingPhoto.set(false);
        },
        error: (err) => {
          this.photoError.set(err.error?.message || 'فشل رفع الصورة');
          this.uploadingPhoto.set(false);
        },
      });
  }

  loadStudent(id: string): void {
    this.loading.set(true);
    this.studentQueriesService
      .getById(id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (data: Student) => {
          this.student.set(data);
          if (data.photoPath) {
            this.studentQueriesService
              .getPhotoBlobUrl(id)
              .pipe(takeUntilDestroyed(this.destroyRef))
              .subscribe((url) => this.studentPhotoUrl.set(url));
          } else {
            this.studentPhotoUrl.set(null);
          }
          this.editForm.set({
            id: data.id,
            fullName: data.fullName,
            phone: data.phone,
            address: data.address || '',
            area: data.area || '',
            college: data.college || '',
            academicYear: data.academicYear || '',
            confessionFather: data.confessionFather || '',
            gender: data.gender ?? 0,
            birthDate: data.birthDate ? fromIsoToDateOnly(data.birthDate) : '',
          });
          this.loadHistory(id);
          this.removalRequestService
            .getPendingForStudent(id)
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe({ next: (req) => this.removalRequest.set(req), error: () => {} });
        },
        error: () => this.loading.set(false),
      });
  }

  loadHistory(studentId: string): void {
    forkJoin({
      calls: this.followUpService.getCallHistory(studentId),
      visits: this.followUpService.getVisitHistory(studentId),
    })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (r) => {
          const calls = Array.isArray(r.calls) ? (r.calls as CallRecord[]) : [];
          const visits = Array.isArray(r.visits) ? (r.visits as VisitRecord[]) : [];
          this.callHistory.set(
            [...calls].sort((a, b) => new Date(b.callDate).getTime() - new Date(a.callDate).getTime())
          );
          this.visitHistory.set(
            [...visits].sort((a, b) => new Date(b.visitDate).getTime() - new Date(a.visitDate).getTime())
          );
          this.loading.set(false);
        },
        error: () => {
          this.callHistory.set([]);
          this.visitHistory.set([]);
          this.loading.set(false);
        },
      });
  }

  reloadHistory(): void {
    const id = this.student()?.id;
    if (id) this.loadHistory(id);
  }

  openAddCall(): void {
    this.callDate.set(new Date().toISOString().slice(0, 16));
    this.callStatus.set(0);
    this.callNotes.set('');
    this.callNextDate.set('');
    this.callError.set('');
    this.showAddCall.set(true);
  }

  closeAddCall(): void {
    this.showAddCall.set(false);
  }

  submitCall(): void {
    const s = this.student();
    if (!s?.id) return;
    this.callSaving.set(true);
    this.callError.set('');
    const payload = {
      studentId: s.id,
      callDate: new Date(this.callDate()).toISOString(),
      callStatus: Number(this.callStatus()),
      notes: this.callNotes() || '',
      nextFollowUpDate: this.callNextDate() ? new Date(this.callNextDate()).toISOString() : null,
    };
    this.followUpService
      .registerCall(payload)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.closeAddCall();
          this.reloadHistory();
          this.callSaving.set(false);
        },
        error: (err) => {
          this.callError.set(err.error?.message || 'فشل تسجيل المكالمة');
          this.callSaving.set(false);
        },
      });
  }

  openAddVisit(): void {
    this.visitDate.set(new Date().toISOString().slice(0, 16));
    this.visitOutcome.set(0);
    this.visitNotes.set('');
    this.visitNextDate.set('');
    this.visitError.set('');
    this.selectedParticipantIds.set([]);
    this.participantSearchQuery.set('');
    this.showAddVisit.set(true);
  }

  closeAddVisit(): void {
    this.showAddVisit.set(false);
  }

  canEditVisit(visit: VisitRecord): boolean {
    const uid = this.authService.currentUser()?.userId;
    return !!uid && (visit as unknown as { recordedByServantId?: string })?.recordedByServantId === uid;
  }

  openEditVisit(visit: VisitRecord): void {
    if (!visit?.id) return;
    this.visitToEdit.set(visit);
    const v = visit as unknown as { visitDate?: string; visitOutcome?: number; notes?: string; nextVisitDate?: string; participants?: { servantId: string }[] };
    this.visitDate.set(v.visitDate ? new Date(v.visitDate).toISOString().slice(0, 16) : new Date().toISOString().slice(0, 16));
    this.visitOutcome.set(Number(v.visitOutcome) ?? 0);
    this.visitNotes.set(v.notes || '');
    this.visitNextDate.set(v.nextVisitDate ? new Date(v.nextVisitDate).toISOString().slice(0, 16) : '');
    const uid = this.authService.currentUser()?.userId;
    this.selectedParticipantIds.set(
      (v.participants || []).filter((p: { servantId: string }) => p.servantId !== uid).map((p: { servantId: string }) => p.servantId)
    );
    this.participantSearchQuery.set('');
    this.visitError.set('');
    this.showEditVisit.set(true);
  }

  closeEditVisit(): void {
    this.showEditVisit.set(false);
    this.visitToEdit.set(null);
  }

  submitEditVisit(): void {
    const visit = this.visitToEdit();
    if (!visit?.id) return;
    this.visitSaving.set(true);
    this.visitError.set('');
    const payload = {
      visitDate: new Date(this.visitDate()).toISOString(),
      visitOutcome: Number(this.visitOutcome()),
      notes: this.visitNotes() || '',
      nextVisitDate: this.visitNextDate() ? new Date(this.visitNextDate()).toISOString() : null,
      participantServantIds: this.selectedParticipantIds().length ? this.selectedParticipantIds() : null,
    };
    this.followUpService
      .updateVisit(visit.id, payload)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.closeEditVisit();
          this.reloadHistory();
          this.visitSaving.set(false);
        },
        error: (err) => {
          this.visitError.set(err.error?.message || 'فشل تحديث الزيارة');
          this.visitSaving.set(false);
        },
      });
  }

  toggleVisitParticipant(servantId: string): void {
    this.selectedParticipantIds.update((ids) => {
      const idx = ids.indexOf(servantId);
      if (idx === -1) return [...ids, servantId];
      const next = [...ids];
      next.splice(idx, 1);
      return next;
    });
  }

  isParticipantSelected(servantId: string): boolean {
    return this.selectedParticipantIds().includes(servantId);
  }

  submitVisit(): void {
    const s = this.student();
    if (!s?.id) return;
    this.visitSaving.set(true);
    this.visitError.set('');
    const payload = {
      studentId: s.id,
      visitDate: new Date(this.visitDate()).toISOString(),
      visitOutcome: Number(this.visitOutcome()),
      notes: this.visitNotes() || '',
      nextVisitDate: this.visitNextDate() ? new Date(this.visitNextDate()).toISOString() : null,
      participantServantIds: this.selectedParticipantIds().length ? this.selectedParticipantIds() : null,
    };
    this.followUpService
      .registerVisit(payload)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.closeAddVisit();
          this.reloadHistory();
          this.visitSaving.set(false);
        },
        error: (err) => {
          this.visitError.set(err.error?.message || 'فشل تسجيل الزيارة');
          this.visitSaving.set(false);
        },
      });
  }

  startEdit(): void {
    const s = this.student();
    if (!s) return;
    this.editForm.set({
      id: s.id,
      fullName: s.fullName,
      phone: s.phone,
      address: s.address || '',
      area: s.area || '',
      college: s.college || '',
      academicYear: s.academicYear || '',
      confessionFather: s.confessionFather || '',
      gender: s.gender ?? 0,
      birthDate: s.birthDate ? String(s.birthDate).split('T')[0] : '',
      servantId: s.servantId,
    });
    this.editing.set(true);
    this.editError.set('');
  }

  cancelEdit(): void {
    this.editing.set(false);
  }

  saveEdit(): void {
    const s = this.student();
    if (!s) return;
    const form = this.editForm();
    this.saveEditLoading.set(true);
    this.editError.set('');
    const req = { ...form, birthDate: (form['birthDate'] as string) || null };
    this.studentCommandsService
      .update(s.id, req)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.student.set({ ...s, ...form });
          this.editing.set(false);
          this.saveEditLoading.set(false);
        },
        error: (err) => {
          this.editError.set(err.error?.message || 'فشل التحديث');
          this.saveEditLoading.set(false);
        },
      });
  }

  requestRemoval(): void {
    if (!this.student()?.id || this.removalRequest()) return;
    this.showRemovalChoiceModal.set(true);
    this.removalError.set('');
  }

  submitRemovalRequest(removalType: number): void {
    if (!this.student()?.id || this.removalRequest()) return;
    this.requestRemovalLoading.set(true);
    this.removalError.set('');
    this.removalRequestService
      .create(this.student()!.id, removalType)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.removalRequest.set({ status: 'Pending' });
          this.requestRemovalLoading.set(false);
          this.showRemovalChoiceModal.set(false);
        },
        error: (err) => {
          this.removalError.set(err.error?.message || 'فشل تقديم الطلب');
          this.requestRemovalLoading.set(false);
        },
      });
  }

  closeRemovalModal(): void {
    this.showRemovalChoiceModal.set(false);
  }

  setEditFormKey(key: string, value: unknown): void {
    this.editForm.update((f) => ({ ...f, [key]: value }));
  }
}
