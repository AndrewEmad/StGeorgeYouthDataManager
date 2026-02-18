import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RemovalRequestService } from '../../services/removal-request.service';

@Component({
  selector: 'app-removal-requests-page',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './removal-requests.page.html',
  styleUrls: ['./removal-requests.page.css']
})
export class RemovalRequestsPage implements OnInit {
  requests: any[] = [];
  loading = true;
  processingId: string | null = null;
  error = '';

  constructor(private removalRequestService: RemovalRequestService) {}

  ngOnInit() {
    this.load();
  }

  load() {
    this.loading = true;
    this.error = '';
    this.removalRequestService.getPending().subscribe({
      next: (data) => {
        this.requests = Array.isArray(data) ? data : [];
        this.loading = false;
      },
      error: () => {
        this.error = 'فشل تحميل الطلبات';
        this.loading = false;
      }
    });
  }

  approve(id: string) {
    this.processingId = id;
    this.error = '';
    this.removalRequestService.approve(id).subscribe({
      next: () => {
        this.requests = this.requests.filter(r => r.id !== id);
        this.processingId = null;
      },
      error: (err) => {
        this.error = err.error?.message || 'فشل الموافقة';
        this.processingId = null;
      }
    });
  }

  reject(id: string) {
    this.processingId = id;
    this.error = '';
    this.removalRequestService.reject(id).subscribe({
      next: () => {
        this.requests = this.requests.filter(r => r.id !== id);
        this.processingId = null;
      },
      error: (err) => {
        this.error = err.error?.message || 'فشل الرفض';
        this.processingId = null;
      }
    });
  }
}
