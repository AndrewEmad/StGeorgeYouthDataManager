import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-servant-header',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './servant-header.html',
  styleUrls: ['./servant-header.css']
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
