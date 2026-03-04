import { Component, OnInit, signal, inject, ChangeDetectionStrategy } from '@angular/core';
import { StudentAdditionRequestService } from '../../services/student-addition-request.service';
import { StudentAdditionRequestDto } from '../../shared/models/request.model';
import { ContentHeaderComponent, LoaderComponent, EmptyStateComponent, RequestCardComponent } from '../../components/common/common';

@Component({
  selector: 'app-student-addition-requests-page',
  standalone: true,
  imports: [ContentHeaderComponent, LoaderComponent, EmptyStateComponent, RequestCardComponent],
  templateUrl: './student-addition-requests.page.html',
  styleUrls: ['./student-addition-requests.page.css'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class StudentAdditionRequestsPage implements OnInit {
  private additionRequestService = inject(StudentAdditionRequestService);

  requests = signal<StudentAdditionRequestDto[]>([]);
  loading = signal(true);
  processingId = signal<string | null>(null);
  error = signal('');

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.error.set('');
    this.additionRequestService.getPending().subscribe({
      next: (data) => {
        this.requests.set(Array.isArray(data) ? data : []);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('فشل تحميل الطلبات');
        this.loading.set(false);
      },
    });
  }

  approve(id: string, assign: boolean = true): void {
    this.processingId.set(id);
    this.error.set('');
    this.additionRequestService.approve(id, assign).subscribe({
      next: () => {
        this.requests.update((list) => list.filter((r) => r.id !== id));
        this.processingId.set(null);
      },
      error: (err: { error?: { message?: string } }) => {
        this.error.set(err.error?.message || 'فشل الموافقة');
        this.processingId.set(null);
      },
    });
  }

  reject(id: string): void {
    this.processingId.set(id);
    this.error.set('');
    this.additionRequestService.reject(id).subscribe({
      next: () => {
        this.requests.update((list) => list.filter((r) => r.id !== id));
        this.processingId.set(null);
      },
      error: (err: { error?: { message?: string } }) => {
        this.error.set(err.error?.message || 'فشل الرفض');
        this.processingId.set(null);
      },
    });
  }
}
