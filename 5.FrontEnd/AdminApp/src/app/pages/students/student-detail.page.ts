import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute } from '@angular/router';
import { StudentQueriesService, StudentEditLogDto } from '../../services/student-queries.service';
import { StudentCommandsService } from '../../services/student-commands.service';
import { FollowUpService, CallLogDto, HomeVisitDto } from '../../services/follow-up.service';
import { ContentHeaderComponent, LoaderComponent, DetailSectionComponent, PhotoCropModalComponent, ProfilePhotoInputComponent } from '../../components/common/common';

@Component({
  selector: 'app-student-detail-page',
  standalone: true,
  imports: [CommonModule, ContentHeaderComponent, LoaderComponent, DetailSectionComponent, PhotoCropModalComponent, ProfilePhotoInputComponent],
  templateUrl: './student-detail.page.html',
  styleUrls: ['./student-detail.page.css']
})
export class StudentDetailPage implements OnInit, OnDestroy {
  student: any = null;
  studentPhotoUrl: string | null = null;
  calls: CallLogDto[] = [];
  visits: HomeVisitDto[] = [];
  editHistory: StudentEditLogDto[] = [];
  loading = true;
  uploadingPhoto = false;
  photoError = '';
  pendingCropFile: File | null = null;

  constructor(
    private route: ActivatedRoute,
    private studentQueries: StudentQueriesService,
    private studentCommands: StudentCommandsService,
    private followUp: FollowUpService
  ) {}

  ngOnInit() {
    const id = this.route.snapshot.paramMap.get('id');
    if (!id) return;
    this.loading = true;
    this.studentQueries.getById(id).subscribe({
      next: (s) => {
        this.student = s;
        if (s.photoPath) {
          this.studentQueries.getPhotoBlobUrl(id).subscribe(url => this.studentPhotoUrl = url);
        }
        this.followUp.getCallHistory(id).subscribe({ next: (c) => this.calls = c || [] });
        this.followUp.getVisitHistory(id).subscribe({ next: (v) => this.visits = v || [] });
        this.studentQueries.getEditHistory(id).subscribe({ next: (h) => this.editHistory = h || [] });
        this.loading = false;
      },
      error: () => this.loading = false
    });
  }

  ngOnDestroy() {
    if (this.studentPhotoUrl) URL.revokeObjectURL(this.studentPhotoUrl);
  }

  onStudentPhotoSelected(file: File) {
    if (!this.student?.id) return;
    this.photoError = '';
    this.pendingCropFile = file;
  }

  onCropConfirm(file: File) {
    this.pendingCropFile = null;
    this.uploadPhoto(file);
  }

  onCropCancel() {
    this.pendingCropFile = null;
  }

  private uploadPhoto(file: File) {
    if (!this.student?.id) return;
    this.uploadingPhoto = true;
    this.studentCommands.uploadPhoto(this.student.id, file).subscribe({
      next: (res) => {
        this.student.photoPath = res.photoPath;
        if (this.studentPhotoUrl) URL.revokeObjectURL(this.studentPhotoUrl);
        this.studentQueries.getPhotoBlobUrl(this.student.id).subscribe(url => this.studentPhotoUrl = url);
        this.uploadingPhoto = false;
      },
      error: (err) => {
        this.photoError = err.error?.message || 'فشل رفع الصورة';
        this.uploadingPhoto = false;
      }
    });
  }

  formatDate(d: string | null): string {
    if (!d) return '—';
    const date = new Date(d);
    return date.toLocaleDateString('ar-EG', { day: '2-digit', month: '2-digit', year: 'numeric' });
  }

  callStatusLabel(n: number): string {
    const map: Record<number, string> = { 0: 'لم يرد', 1: 'رد', 2: 'مشغول', 3: 'مغلق', 4: 'رقم خاطئ' };
    return map[n] ?? String(n);
  }

  visitOutcomeLabel(n: number): string {
    const map: Record<number, string> = { 0: 'تمت الزيارة', 1: 'غير موجود', 2: 'رفض الاستقبال', 3: 'مؤجلة' };
    return map[n] ?? String(n);
  }
}
