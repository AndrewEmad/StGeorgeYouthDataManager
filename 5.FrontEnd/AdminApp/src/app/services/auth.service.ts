import { Injectable, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { Observable, tap } from 'rxjs';
import { environment } from '../../environments/environment';

export interface LoginResponse {
  token: string;
  refreshToken: string;
  fullName: string;
  role: string;
  userId: string;
  requiresPasswordChange?: boolean;
}

@Injectable({
  providedIn: 'root',
})
export class AuthService {
  private apiUrl = `${environment.apiUrl}/Auth`;
  currentUser = signal<LoginResponse | null>(null);

  constructor(
    private http: HttpClient,
    private router: Router,
  ) {
    const savedUser = localStorage.getItem('user');
    if (savedUser) {
      const user = JSON.parse(savedUser);
      if (user.role === 'Admin' || user.role === 'Priest' || user.role === 'Manager') {
        this.currentUser.set(user);
        if (this.isTokenExpired()) {
          localStorage.removeItem('user');
          localStorage.removeItem('token');
          this.currentUser.set(null);
        }
      } else {
        this.logout();
      }
    }
  }

  login(credentials: { username: string; password: string }): Observable<LoginResponse> {
    return this.http.post<LoginResponse>(`${this.apiUrl}/login`, credentials).pipe(
      tap((response) => {
        if (response.role !== 'Admin' && response.role !== 'Priest' && response.role !== 'Manager') {
          throw new Error('Unauthorized');
        }
        localStorage.setItem('user', JSON.stringify(response));
        localStorage.setItem('token', response.token);
        this.currentUser.set(response);
      }),
    );
  }

  setUserAfterPasswordChange(response: LoginResponse): void {
    localStorage.setItem('user', JSON.stringify(response));
    localStorage.setItem('token', response.token);
    this.currentUser.set(response);
  }

  requiresPasswordChange(): boolean {
    return !!this.currentUser()?.requiresPasswordChange;
  }

  changePassword(currentPassword: string, newPassword: string): Observable<LoginResponse> {
    return this.http.post<LoginResponse>(`${this.apiUrl}/change-password`, {
      currentPassword,
      newPassword,
    });
  }

  logout() {
    localStorage.removeItem('user');
    localStorage.removeItem('token');
    this.currentUser.set(null);
    this.router.navigate(['/login']);
  }

  isLoggedIn(): boolean {
    return !!this.currentUser();
  }

  hasRequiredRole(): boolean {
    const r = this.currentUser()?.role;
    return r === 'Admin' || r === 'Priest' || r === 'Manager';
  }

  isTokenExpired(): boolean {
    const token = localStorage.getItem('token');
    if (!token) return true;
    try {
      const payload = token.split('.')[1];
      if (!payload) return false;
      const decoded = JSON.parse(atob(payload.replace(/-/g, '+').replace(/_/g, '/')));
      const exp = decoded?.exp as number | undefined;
      if (exp == null) return false;
      return exp * 1000 - 30 * 1000 < Date.now();
    } catch {
      return false;
    }
  }

  shouldRedirectToHome(): boolean {
    return this.isLoggedIn() && this.hasRequiredRole() && !this.isTokenExpired();
  }
}
