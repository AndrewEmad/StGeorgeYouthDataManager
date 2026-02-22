import { Component, input } from '@angular/core';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-back-link',
  standalone: true,
  imports: [RouterLink],
  templateUrl: './back-link.component.html',
  styleUrl: './back-link.component.css'
})
export class BackLinkComponent {
  route = input.required<string | string[]>();
  label = input<string>('العودة');
}
