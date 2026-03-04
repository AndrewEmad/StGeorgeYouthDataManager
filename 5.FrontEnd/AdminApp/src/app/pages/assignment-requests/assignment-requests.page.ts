import { Component, OnInit, signal, inject, ChangeDetectionStrategy } from '@angular/core';
import { AssignmentRequestService } from '../../services/assignment-request.service';
import { StudentAssignmentRequestDto } from '../../shared/models/request.model';
import { ContentHeaderComponent, LoaderComponent, EmptyStateComponent, RequestCardComponent } from '../../components/common/common';

@Component({
  selector: 'app-assignment-requests-page',
  standalone: true,
  imports: [ContentHeaderComponent, LoaderComponent, EmptyStateComponent, RequestCardComponent],
  templateUrl: './assignment-requests.page.html',
  styleUrls: ['./assignment-requests.page.css'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AssignmentRequestsPage implements OnInit {
  private assignmentRequestService = inject(AssignmentRequestService);

  requests = signal<StudentAssignmentRequestDto[]>([]);
  loading = signal(true);
  processingId = signal<string | null>(null);
  error = signal('');

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.error.set('');
    this.assignmentRequestService.getPending().subscribe({
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

  approve(id: string): void {
    this.processingId.set(id);
    this.error.set('');
    this.assignmentRequestService.approve(id).subscribe({
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
    this.assignmentRequestService.reject(id).subscribe({
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
