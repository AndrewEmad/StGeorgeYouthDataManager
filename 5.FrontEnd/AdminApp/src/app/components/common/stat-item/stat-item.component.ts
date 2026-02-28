import { Component, input } from '@angular/core';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-stat-item',
  standalone: true,
  imports: [RouterLink],
  templateUrl: './stat-item.component.html',
  styleUrl: './stat-item.component.css'
})
export class StatItemComponent {
  icon = input.required<string>();
  value = input<string | number>('');
  label = input.required<string>();
  variant = input<string>('');
  routerLink = input<string | unknown[] | null>(null);
}
