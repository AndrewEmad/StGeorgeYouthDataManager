import { Component } from '@angular/core';
import { ServantListComponent } from '../../components/servants/servant-list/servant-list';

@Component({
  selector: 'app-servants-page',
  standalone: true,
  imports: [ServantListComponent],
  templateUrl: './servants.page.html'
})
export class ServantsPage {}
