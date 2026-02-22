import { Component, input } from '@angular/core';

@Component({
  selector: 'app-detail-section',
  standalone: true,
  templateUrl: './detail-section.component.html',
  styleUrl: './detail-section.component.css'
})
export class DetailSectionComponent {
  title = input.required<string>();
}
