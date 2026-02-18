import { Component } from '@angular/core';
import { StatsComponent } from '../../components/dashboard/stats';

@Component({
  selector: 'app-stats-page',
  standalone: true,
  imports: [StatsComponent],
  templateUrl: './stats.page.html'
})
export class StatsPage {}
