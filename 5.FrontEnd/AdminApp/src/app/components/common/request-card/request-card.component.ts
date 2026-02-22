import { Component, input } from '@angular/core';
import { DatePipe } from '@angular/common';

@Component({
  selector: 'app-request-card',
  standalone: true,
  imports: [DatePipe],
  templateUrl: './request-card.component.html',
  styleUrl: './request-card.component.css'
})
export class RequestCardComponent {
  title = input.required<string>();
  subtitle = input<string | null>(null);
  date = input<Date | string | null>(null);
}
