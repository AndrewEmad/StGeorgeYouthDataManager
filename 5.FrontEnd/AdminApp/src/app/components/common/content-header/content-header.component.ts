import { Component, input } from '@angular/core';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-content-header',
  standalone: true,
  imports: [RouterLink],
  templateUrl: './content-header.component.html',
  styleUrl: './content-header.component.css'
})
export class ContentHeaderComponent {
  title = input<string>('');
  subtitle = input<string | null>(null);
  backLink = input<string | string[] | null>(null);
  backLabel = input<string>('العودة');
}
