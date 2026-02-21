import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { AddToHomeScreenComponent } from './components/add-to-home-screen/add-to-home-screen.component';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet, AddToHomeScreenComponent],
  templateUrl: './app.component.html'
})
export class AppComponent {
  title = 'AdminApp';
}
