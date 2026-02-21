import { Component, OnInit } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { AddToHomeScreenComponent } from './components/add-to-home-screen/add-to-home-screen.component';
import { AuthService } from './services/auth.service';
import { ReminderService } from './services/reminder.service';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet, AddToHomeScreenComponent],
  template: `<router-outlet></router-outlet><app-add-to-home-screen />`,
  styles: []
})
export class AppComponent implements OnInit {
  title = 'ServantApp';

  constructor(
    private authService: AuthService,
    private reminderService: ReminderService
  ) {}

  ngOnInit(): void {
    if (this.authService.currentUser()) {
      this.reminderService.startChecking();
    }
  }
}
