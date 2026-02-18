import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReportsService } from '../../../services/reports.service';

@Component({
  selector: 'app-backups',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './backups.html',
  styleUrls: ['./backups.css']
})
export class BackupsComponent {
  constructor(private reportsService: ReportsService) {}

  exportStudents() {
    this.reportsService.exportStudents({}).subscribe((blob: Blob) => {
      this.downloadFile(blob, 'All_Students_Report.xlsx');
    });
  }

  exportCalls() {
    this.reportsService.exportCalls({}).subscribe((blob: Blob) => {
      this.downloadFile(blob, 'Call_Logs_Report.xlsx');
    });
  }

  exportVisits() {
    this.reportsService.exportVisits({}).subscribe((blob: Blob) => {
      this.downloadFile(blob, 'Home_Visits_Report.xlsx');
    });
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
