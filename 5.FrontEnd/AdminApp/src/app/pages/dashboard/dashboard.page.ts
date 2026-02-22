import { Component } from '@angular/core';
import { LayoutComponent } from '../../components/layout/layout';

@Component({
  selector: 'app-dashboard-page',
  standalone: true,
  imports: [LayoutComponent],
  templateUrl: './dashboard.page.html'
})
export class DashboardPage {}
