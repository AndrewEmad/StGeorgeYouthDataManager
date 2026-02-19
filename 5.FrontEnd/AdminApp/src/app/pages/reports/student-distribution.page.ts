import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { ReportsService, StudentsByGroup } from '../../services/reports.service';

@Component({
  selector: 'app-student-distribution-page',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './student-distribution.page.html',
  styleUrls: ['./servant-follow-up.page.css']
})
export class StudentDistributionPage implements OnInit {
  byArea: StudentsByGroup[] = [];
  byAcademicYear: StudentsByGroup[] = [];
  loadingArea = false;
  loadingYear = false;
  errorArea = '';
  errorYear = '';

  constructor(private reportsService: ReportsService) {}

  ngOnInit() {
    this.loadArea();
    this.loadYear();
  }

  loadArea() {
    this.loadingArea = true;
    this.errorArea = '';
    this.reportsService.getStudentsByArea().subscribe({
      next: (data) => {
        this.byArea = data;
        this.loadingArea = false;
      },
      error: () => {
        this.errorArea = 'فشل تحميل التقرير';
        this.loadingArea = false;
      }
    });
  }

  loadYear() {
    this.loadingYear = true;
    this.errorYear = '';
    this.reportsService.getStudentsByAcademicYear().subscribe({
      next: (data) => {
        this.byAcademicYear = data;
        this.loadingYear = false;
      },
      error: () => {
        this.errorYear = 'فشل تحميل التقرير';
        this.loadingYear = false;
      }
    });
  }
}
