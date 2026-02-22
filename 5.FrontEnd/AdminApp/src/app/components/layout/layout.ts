import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterLink, RouterOutlet, RouterLinkActive, ActivatedRoute, NavigationEnd } from '@angular/router';
import { filter } from 'rxjs/operators';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-layout',
  standalone: true,
  imports: [CommonModule, RouterLink, RouterOutlet, RouterLinkActive],
  templateUrl: './layout.html',
  styleUrls: ['./layout.css']
})
export class LayoutComponent implements OnInit, OnDestroy {
  adminName = '';
  sidebarOpen = false;
  pageTitle = '';
  private sub?: ReturnType<Router['events']['subscribe']>;

  constructor(
    private authService: AuthService,
    private router: Router,
    private route: ActivatedRoute
  ) {
    this.adminName = this.authService.currentUser()?.fullName || 'المدير';
  }

  ngOnInit() {
    this.setTitleFromRoute();
    this.sub = this.router.events
      .pipe(filter((e): e is NavigationEnd => e instanceof NavigationEnd))
      .subscribe(() => this.setTitleFromRoute());
  }

  private setTitleFromRoute() {
    let r: ActivatedRoute | null = this.route;
    while (r?.firstChild) r = r.firstChild;
    this.pageTitle = r?.snapshot?.data?.['title'] ?? '';
  }

  ngOnDestroy() {
    this.sub?.unsubscribe();
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
