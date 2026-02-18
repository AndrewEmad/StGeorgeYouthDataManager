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
      this.currentUser.set(JSON.parse(savedUser));
    }
  }

  login(credentials: { username: string; password: string }): Observable<LoginResponse> {
    return this.http.post<LoginResponse>(`${this.apiUrl}/login`, credentials).pipe(
      tap(response => {
        if (response.role === 'Admin') {
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

  getToken(): string | null {
    return localStorage.getItem('token');
  }
}
