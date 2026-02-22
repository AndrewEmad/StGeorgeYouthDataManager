import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterLink, RouterOutlet, ActivatedRoute, NavigationEnd } from '@angular/router';
import { filter } from 'rxjs/operators';
import { Subscription } from 'rxjs';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-servant-layout',
  standalone: true,
  imports: [CommonModule, RouterLink, RouterOutlet],
  templateUrl: './servant-layout.component.html',
  styleUrls: ['./servant-layout.component.css']
})
export class ServantLayoutComponent implements OnInit, OnDestroy {
  pageTitle = '';
  private sub: Subscription | null = null;

  constructor(
    public authService: AuthService,
    private router: Router,
    private route: ActivatedRoute
  ) {}

  ngOnInit(): void {
    this.updateTitle();
    this.sub = this.router.events.pipe(
      filter((e): e is NavigationEnd => e instanceof NavigationEnd)
    ).subscribe(() => this.updateTitle());
  }

  ngOnDestroy(): void {
    this.sub?.unsubscribe();
  }

  private updateTitle(): void {
    let r: ActivatedRoute | null = this.route;
    while (r?.firstChild) r = r.firstChild;
    this.pageTitle = (r?.snapshot?.data && r.snapshot.data['title']) ? r.snapshot.data['title'] : '';
  }

  get isDashboard(): boolean {
    const url = this.router.url;
    return url === '/dashboard' || url === '/dashboard/' || url === '';
  }

  get servantName(): string {
    return this.authService.currentUser()?.fullName ?? '';
  }

  logout(): void {
    this.authService.logout();
  }
}
