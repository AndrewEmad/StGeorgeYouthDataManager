import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { AddToHomeScreenComponent } from './components/add-to-home-screen/add-to-home-screen.component';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet, AddToHomeScreenComponent],
  template: `<router-outlet></router-outlet><app-add-to-home-screen />`,
  styles: []
})
export class AppComponent {
  title = 'ServantApp';
}
