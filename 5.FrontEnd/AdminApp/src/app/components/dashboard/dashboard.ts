import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterLink, RouterOutlet, RouterLinkActive } from '@angular/router';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, RouterLink, RouterOutlet, RouterLinkActive],
  templateUrl: './dashboard.html',
  styleUrls: ['./dashboard.css']
})
export class DashboardComponent implements OnInit, OnDestroy {
  adminName = '';
  sidebarOpen = false;

  constructor(private authService: AuthService, private router: Router) {
    this.adminName = this.authService.currentUser()?.fullName || 'المدير';
  }

  ngOnInit() {}

  ngOnDestroy() {
    document.body.classList.remove('sidebar-open-mobile');
  }

  toggleSidebar() {
    this.sidebarOpen = !this.sidebarOpen;
    this.updateBodyScrollLock();
  }

  closeSidebar() {
    this.sidebarOpen = false;
    this.updateBodyScrollLock();
  }

  private updateBodyScrollLock() {
    if (this.sidebarOpen) {
      document.body.classList.add('sidebar-open-mobile');
    } else {
      document.body.classList.remove('sidebar-open-mobile');
    }
  }

  logout() {
    this.authService.logout();
  }
}
