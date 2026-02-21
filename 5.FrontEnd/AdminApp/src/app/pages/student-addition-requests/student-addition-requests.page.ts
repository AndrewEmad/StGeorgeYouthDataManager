import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { StudentAdditionRequestService } from '../../services/student-addition-request.service';

@Component({
  selector: 'app-student-addition-requests-page',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './student-addition-requests.page.html',
  styleUrls: ['./student-addition-requests.page.css']
})
export class StudentAdditionRequestsPage implements OnInit {
  requests: any[] = [];
  loading = true;
  processingId: string | null = null;
  error = '';

  constructor(private additionRequestService: StudentAdditionRequestService) {}

  ngOnInit() {
    this.load();
  }

  load() {
    this.loading = true;
    this.error = '';
    this.additionRequestService.getPending().subscribe({
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
    this.additionRequestService.approve(id).subscribe({
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
    this.additionRequestService.reject(id).subscribe({
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
