import { Component, input } from '@angular/core';

@Component({
  selector: 'app-stat-item',
  standalone: true,
  templateUrl: './stat-item.component.html',
  styleUrl: './stat-item.component.css'
})
export class StatItemComponent {
  icon = input.required<string>();
  value = input<string | number>('');
  label = input.required<string>();
  variant = input<string>('');
}
