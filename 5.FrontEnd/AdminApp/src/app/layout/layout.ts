import { Component, OnInit, OnDestroy } from '@angular/core';
import { Router, RouterLink, RouterOutlet, RouterLinkActive, ActivatedRoute, NavigationEnd } from '@angular/router';
import {NgTemplateOutlet} from '@angular/common';
import {ModalComponent} from '@ydm/shared/ui/modal';
import { filter } from 'rxjs/operators';
import { AuthService } from '../core/services/auth.service';

@Component({
  selector: 'app-layout',
  standalone: true,
  imports: [RouterLink, RouterOutlet, RouterLinkActive,NgTemplateOutlet,ModalComponent],
  templateUrl: './layout.html',
  styleUrls: ['./layout.css'],
})
export class LayoutComponent implements OnInit, OnDestroy {
  get adminName(){return this.authService.currentUser()?.fullName || 'الإدارة';}
  mobile=matchMedia('(max-width:768px)').matches || matchMedia('(display-mode:standalone)').matches;
  private media=matchMedia('(max-width:768px)');
  private resize=()=>{this.mobile=this.media.matches || matchMedia('(display-mode:standalone)').matches;if(!this.mobile)this.closeSidebar();};
  sidebarOpen = false;
  pageTitle = '';
  private sub?: ReturnType<Router['events']['subscribe']>;

  constructor(
    private authService: AuthService,
    private router: Router,
    private route: ActivatedRoute
  ) {
    this.media.addEventListener('change',this.resize);
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
    this.sub?.unsubscribe();this.media.removeEventListener('change',this.resize);
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
