import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReportsService } from '../../../services/reports.service';
import { ContentHeaderComponent, CardComponent } from '../../common/common';

@Component({
  selector: 'app-backups',
  standalone: true,
  imports: [CommonModule, ContentHeaderComponent, CardComponent],
  templateUrl: './backups.html',
  styleUrls: ['./backups.css']
})
export class BackupsComponent {
  constructor(private reportsService: ReportsService) {}

  exportStudents() {
    this.reportsService.exportStudents({}).subscribe((blob: Blob) => {
      this.downloadFile(blob, this.arabicFileName('تقرير_المخدومين'));
    });
  }

  exportCalls() {
    this.reportsService.exportCalls({}).subscribe((blob: Blob) => {
      this.downloadFile(blob, this.arabicFileName('تقرير_المكالمات'));
    });
  }

  exportVisits() {
    this.reportsService.exportVisits({}).subscribe((blob: Blob) => {
      this.downloadFile(blob, this.arabicFileName('تقرير_الزيارات'));
    });
  }

  private arabicFileName(baseName: string): string {
    const now = new Date();
    const d = now.getDate().toString().padStart(2, '0');
    const m = (now.getMonth() + 1).toString().padStart(2, '0');
    const y = now.getFullYear();
    const h = now.getHours().toString().padStart(2, '0');
    const min = now.getMinutes().toString().padStart(2, '0');
    const sec = now.getSeconds().toString().padStart(2, '0');
    return `${baseName}_${y}${m}${d}${h}${min}${sec}.xlsx`;
  }

  private downloadFile(blob: Blob, fileName: string) {
    const url = window.URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = fileName;
    a.click();
    window.URL.revokeObjectURL(url);
  }
}
