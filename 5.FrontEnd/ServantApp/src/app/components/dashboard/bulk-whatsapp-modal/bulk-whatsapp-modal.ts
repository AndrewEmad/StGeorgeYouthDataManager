import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { StudentQueriesService } from '../../../services/student-queries.service';

@Component({
  selector: 'app-bulk-whatsapp-modal',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './bulk-whatsapp-modal.html',
  styleUrls: ['./bulk-whatsapp-modal.css']
})
export class BulkWhatsAppModalComponent {
  @Input() userId: string | undefined;
  @Output() close = new EventEmitter<void>();

  step: 1 | 2 = 1;
  message = '';
  assignedForBulk: { id: string; fullName: string; phone: string }[] = [];
  loading = false;

  constructor(private studentQueriesService: StudentQueriesService) {}

  onClose() {
    this.close.emit();
  }

  nextStep() {
    if (!this.userId) return;
    this.loading = true;
    this.studentQueriesService.getByServantId(this.userId).subscribe({
      next: (list) => {
        this.assignedForBulk = (list || [])
          .filter((s: any) => s?.phone)
          .map((s: any) => ({
            id: s.id,
            fullName: s.fullName || '—',
            phone: s.phone
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

  getWaMeUrl(phone: string, text: string): string {
    const digits = (phone || '').replace(/\D/g, '');
    if (!digits) return '#';
    const q = text?.trim() ? `?text=${encodeURIComponent(text.trim())}` : '';
    return `https://wa.me/2${digits}${q}`;
  }
}
