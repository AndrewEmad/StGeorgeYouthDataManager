import { Component, input, output } from '@angular/core';

@Component({
  selector: 'app-filters-bar',
  standalone: true,
  templateUrl: './filters-bar.component.html',
  styleUrl: './filters-bar.component.css'
})
export class FiltersBarComponent {
  open = input<boolean>(false);
  count = input<string | null>(null);
  toggleLabel = input<string>('بحث متقدم');
  openChange = output<void>();

  toggle() {
    this.openChange.emit();
  }
}
