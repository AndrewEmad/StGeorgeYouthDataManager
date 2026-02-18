import { Component } from '@angular/core';
import { DashboardComponent } from '../../components/dashboard/dashboard';

@Component({
  selector: 'app-dashboard-page',
  standalone: true,
  imports: [DashboardComponent],
  templateUrl: './dashboard.page.html'
})
export class DashboardPage {}
