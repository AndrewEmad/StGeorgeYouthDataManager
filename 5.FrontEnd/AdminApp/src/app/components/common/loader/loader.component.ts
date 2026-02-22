import { Component, input } from '@angular/core';

@Component({
  selector: 'app-loader',
  standalone: true,
  templateUrl: './loader.component.html',
  styleUrl: './loader.component.css'
})
export class LoaderComponent {
  message = input<string>('جاري التحميل…');
  fullScreen = input<boolean>(false);
}
