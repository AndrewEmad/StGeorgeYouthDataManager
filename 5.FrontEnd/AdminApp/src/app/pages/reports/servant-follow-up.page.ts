import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';

interface ReportCard {
  title: string;
  icon: string;
  route: string;
}

@Component({
  selector: 'app-servant-follow-up-page',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './servant-follow-up.page.html',
  styleUrls: ['./servant-follow-up.page.css']
})
export class ServantFollowUpPage {
  reportCards: ReportCard[] = [
    { title: 'ملخص نشاط الخدام', icon: 'assessment', route: '/dashboard/reports/servant-activity' },
    { title: 'مخدومون بدون تواصل حديث', icon: 'contact_phone', route: '/dashboard/reports/students-no-contact' },
    { title: 'توزيع المخدومين حسب المنطقة', icon: 'map', route: '/dashboard/reports/students-by-area' },
    { title: 'توزيع المخدومين حسب السنة الدراسية', icon: 'school', route: '/dashboard/reports/students-by-year' }
  ];
}
