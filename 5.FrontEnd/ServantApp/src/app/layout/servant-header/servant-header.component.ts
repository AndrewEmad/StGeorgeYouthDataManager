import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';

@Component({
  selector: 'app-servant-header',
  standalone: true,
  imports: [RouterLink],
  templateUrl: './servant-header.component.html',
  styleUrls: ['./servant-header.component.css'],
})
export class ServantHeaderComponent {
  constructor(public authService: AuthService) {}

  get servantName(): string {
    return this.authService.currentUser()?.fullName ?? '';
  }

  logout(): void {
    this.authService.logout();
  }
}
