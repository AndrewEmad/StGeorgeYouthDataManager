import { Component } from '@angular/core';
import { BackupsComponent } from '../../components/backups/backups/backups';

@Component({
  selector: 'app-backups-page',
  standalone: true,
  imports: [BackupsComponent],
  templateUrl: './backups.page.html'
})
export class BackupsPage {}
