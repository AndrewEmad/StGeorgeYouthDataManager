import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { ReportsService, ServantActivitySummary, StudentNoContact } from '../../services/reports.service';
import { UsersService, User } from '../../services/users.service';

@Component({
  selector: 'app-servant-follow-up-page',
  standalone: true,
  imports: [CommonModule, RouterLink, FormsModule],
  templateUrl: './servant-follow-up.page.html',
  styleUrls: ['./servant-follow-up.page.css']
})
export class ServantFollowUpPage implements OnInit {
  servants: User[] = [];
  activitySummary: ServantActivitySummary[] = [];
  noContactList: StudentNoContact[] = [];
  loadingActivity = false;
  loadingNoContact = false;
  errorActivity = '';
  errorNoContact = '';

  dateFrom = '';
  dateTo = '';
  servantIdFilter = '';
  daysNoContact = 14;
  servantIdNoContact = '';

  constructor(
    private reportsService: ReportsService,
    private usersService: UsersService
  ) {}

  ngOnInit() {
    this.usersService.getPaged({ page: 1, pageSize: 500, role: 'Servant' }).subscribe({
      next: (res) => {
        this.servants = res.items ?? [];
      }
    });
    this.loadActivity();
    this.loadNoContact();
  }

  loadActivity() {
    this.loadingActivity = true;
    this.errorActivity = '';
    const params: { dateFrom?: string; dateTo?: string; servantId?: string } = {};
    if (this.dateFrom) params.dateFrom = this.dateFrom;
    if (this.dateTo) params.dateTo = this.dateTo;
    if (this.servantIdFilter) params.servantId = this.servantIdFilter;
    this.reportsService.getServantActivitySummary(params).subscribe({
      next: (data) => {
        this.activitySummary = data;
        this.loadingActivity = false;
      },
      error: () => {
        this.errorActivity = 'فشل تحميل التقرير';
        this.loadingActivity = false;
      }
    });
  }

  loadNoContact() {
    this.loadingNoContact = true;
    this.errorNoContact = '';
    const params: { days?: number; servantId?: string } = { days: this.daysNoContact };
    if (this.servantIdNoContact) params.servantId = this.servantIdNoContact;
    this.reportsService.getStudentsWithNoRecentContact(params).subscribe({
      next: (data) => {
        this.noContactList = data;
        this.loadingNoContact = false;
      },
      error: () => {
        this.errorNoContact = 'فشل تحميل القائمة';
        this.loadingNoContact = false;
      }
    });
  }

  onApplyActivityFilters() {
    this.loadActivity();
  }

  onApplyNoContactFilters() {
    this.loadNoContact();
  }
}
