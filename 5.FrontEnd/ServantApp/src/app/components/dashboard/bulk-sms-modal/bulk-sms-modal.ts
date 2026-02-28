import { Component, Input, Output, EventEmitter, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { StudentQueriesService } from '../../../services/student-queries.service';

@Component({
  selector: 'app-bulk-sms-modal',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './bulk-sms-modal.html',
  styleUrls: ['./bulk-sms-modal.css']
})
export class BulkSmsModalComponent implements OnInit {
  @Input() userId: string | undefined;
  @Output() close = new EventEmitter<void>();

  step: 1 | 2 = 1;
  message = '';
  assignedForBulkSms: { id: string; fullName: string; phone: string; selected?: boolean }[] = [];
  loading = false;
  isAndroid = false;

  constructor(private studentQueriesService: StudentQueriesService) {}

  ngOnInit() {
    this.checkPlatform();
  }

  checkPlatform() {
    if (typeof navigator !== 'undefined') {
      this.isAndroid = /Android/i.test(navigator.userAgent);
    }
  }

  onClose() {
    this.close.emit();
  }

  nextStep() {
    if (!this.userId) return;
    this.loading = true;
    this.studentQueriesService.getByServantId(this.userId).subscribe({
      next: (list) => {
        this.assignedForBulkSms = (list || [])
          .filter((s: any) => s?.phone)
          .map((s: any) => ({
            id: s.id,
            fullName: s.fullName || '—',
            phone: s.phone,
            selected: true
          }));
        this.loading = false;
        this.step = 2;
      },
      error: () => { this.loading = false; }
    });
  }

  prevStep() {
    this.step = 1;
  }

  toggleSmsSelection(studentId: string) {
    const s = this.assignedForBulkSms.find(x => x.id === studentId);
    if (s) s.selected = !s.selected;
  }

  getSmsUrl(phone: string, text: string): string {
    const digits = (phone || '').replace(/\D/g, '');
    if (!digits) return '#';
    const q = text?.trim() ? `?body=${encodeURIComponent(text.trim())}` : '';
    return `sms:${digits}${q}`;
  }

  sendBulkSmsAndroid() {
    const selected = this.assignedForBulkSms.filter(s => s.selected);
    if (selected.length === 0) return;
    const phones = selected.map(s => s.phone.replace(/\D/g, '')).join(',');
    const q = this.message?.trim() ? `?body=${encodeURIComponent(this.message.trim())}` : '';
    const url = `sms:${phones}${q}`;
    window.location.href = url;
  }
}
