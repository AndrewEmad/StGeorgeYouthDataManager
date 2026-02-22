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
  providedIn: 'root'
})
export class AuthService {
  private apiUrl = `${environment.apiUrl}/Auth`;
  currentUser = signal<LoginResponse | null>(null);

  constructor(private http: HttpClient, private router: Router) {
    const savedUser = localStorage.getItem('user');
    if (savedUser) {
      const user = JSON.parse(savedUser);
      this.currentUser.set(user);
      if (user.role === 'Admin' || user.role === 'Priest' || this.isTokenExpired()) {
        localStorage.removeItem('user');
        localStorage.removeItem('token');
        this.currentUser.set(null);
      }
    }
  }

  login(credentials: { username: string; password: string }): Observable<LoginResponse> {
    return this.http.post<LoginResponse>(`${this.apiUrl}/login`, credentials).pipe(
      tap(response => {
        if (response.role === 'Admin' || response.role === 'Priest') {
          throw new Error('Unauthorized');
        }
        localStorage.setItem('user', JSON.stringify(response));
        localStorage.setItem('token', response.token);
        this.currentUser.set(response);
      })
    );
  }

  logout() {
    localStorage.removeItem('user');
    localStorage.removeItem('token');
    this.currentUser.set(null);
    this.router.navigate(['/login']);
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
    return this.http.post<LoginResponse>(`${this.apiUrl}/change-password`, { currentPassword, newPassword });
  }

  isLoggedIn(): boolean {
    return !!this.currentUser();
  }

  hasRequiredRole(): boolean {
    const r = this.currentUser()?.role;
    return r != null && r !== 'Admin' && r !== 'Priest';
  }

  isSecretary(): boolean {
    return this.currentUser()?.role === 'Secretary';
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

  getToken(): string | null {
    return localStorage.getItem('token');
  }

  getProfile(): Observable<ProfileDto> {
    return this.http.get<ProfileDto>(`${environment.apiUrl}/Profile`);
  }

  updateProfile(dto: { fullName?: string; email?: string; phone?: string }): Observable<void> {
    return this.http.put<void>(`${environment.apiUrl}/Profile`, dto);
  }

  refreshCurrentUserFullName(fullName: string): void {
    const u = this.currentUser();
    if (u) {
      const updated = { ...u, fullName };
      localStorage.setItem('user', JSON.stringify(updated));
      this.currentUser.set(updated);
    }
  }
}

export interface ProfileDto {
  id: string;
  userName: string;
  fullName: string;
  email: string;
  phone: string;
  role: string;
  isActive: boolean;
  createdAt: string;
  updatedAt: string | null;
}
