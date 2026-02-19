import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AssignmentRequestService } from '../../services/assignment-request.service';

@Component({
  selector: 'app-assignment-requests-page',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './assignment-requests.page.html',
  styleUrls: ['./assignment-requests.page.css']
})
export class AssignmentRequestsPage implements OnInit {
  requests: any[] = [];
  loading = true;
  processingId: string | null = null;
  error = '';

  constructor(private assignmentRequestService: AssignmentRequestService) {}

  ngOnInit() {
    this.load();
  }

  load() {
    this.loading = true;
    this.error = '';
    this.assignmentRequestService.getPending().subscribe({
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
    this.assignmentRequestService.approve(id).subscribe({
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
    this.assignmentRequestService.reject(id).subscribe({
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
