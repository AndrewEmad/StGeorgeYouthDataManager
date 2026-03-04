import { Component, OnInit, signal, inject, ChangeDetectionStrategy } from '@angular/core';
import { RemovalRequestService } from '../../services/removal-request.service';
import { StudentRemovalRequestDto } from '../../shared/models/request.model';
import { ContentHeaderComponent, LoaderComponent, EmptyStateComponent, RequestCardComponent } from '../../components/common/common';

@Component({
  selector: 'app-removal-requests-page',
  standalone: true,
  imports: [ContentHeaderComponent, LoaderComponent, EmptyStateComponent, RequestCardComponent],
  templateUrl: './removal-requests.page.html',
  styleUrls: ['./removal-requests.page.css'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RemovalRequestsPage implements OnInit {
  private removalRequestService = inject(RemovalRequestService);

  requests = signal<StudentRemovalRequestDto[]>([]);
  loading = signal(true);
  processingId = signal<string | null>(null);
  error = signal('');

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.error.set('');
    this.removalRequestService.getPending().subscribe({
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
    this.removalRequestService.approve(id).subscribe({
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
    this.removalRequestService.reject(id).subscribe({
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
