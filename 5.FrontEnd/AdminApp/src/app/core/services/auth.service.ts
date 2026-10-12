import { Injectable, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { Observable, from, firstValueFrom, tap, finalize, shareReplay } from 'rxjs';
import { environment } from '../../../environments/environment';
import type { LoginResponse, ProfileDto } from '../../shared/models/auth.model';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly key = 'ydm.adminapp.user';
  private readonly tokenKey = 'ydm.adminapp.token';
  private readonly apiUrl = `${environment.apiUrl}/Auth`;
  private refreshing?: Observable<LoginResponse>;
  currentUser = signal<LoginResponse | null>(null);
  constructor(private http: HttpClient, private router: Router) {
    this.readSession();
    this.flushRevocations();window.addEventListener('online',()=>this.flushRevocations());
    window.addEventListener('storage', e => {
      if (e.key !== this.key) return;
      const previous = this.currentUser()?.userId;
      this.readSession();
      if (previous !== this.currentUser()?.userId || !this.currentUser()) this.router.navigate(['/login']);
    });
  }
  private readSession(): void {
    try {
      const session = JSON.parse(localStorage.getItem(this.key) || 'null');
      if (session && typeof session.token === 'string' && typeof session.refreshToken === 'string' &&
          typeof session.userId === 'string' && ['Admin', 'Priest', 'Manager'].includes(session.role) && this.validToken(session.token)) {
        this.currentUser.set(session); return;
      }
    } catch { /* Corrupt session is cleared below. */ }
    this.clearSession();
  }
  private validToken(token: string): boolean {
    try { const claims = JSON.parse(atob(token.split('.')[1].replace(/-/g, '+').replace(/_/g, '/'))); return Number.isFinite(claims.exp); }
    catch { return false; }
  }
  acceptSession(session: LoginResponse): void {
    if (!session?.token || !session.refreshToken || !this.validToken(session.token) || !['Admin', 'Priest', 'Manager'].includes(session.role)) throw new Error('Invalid account session');
    const previous = this.currentUser()?.userId;
    if (previous && previous !== session.userId) {this.revokeSession(this.currentUser()!.refreshToken,localStorage.getItem('ydm.adminapp.fcm_token'));this.clearSession();}
    localStorage.setItem(this.key, JSON.stringify(session));
    localStorage.setItem(this.tokenKey, session.token);
    this.currentUser.set(session);
  }
  clearSession(): void {
    localStorage.removeItem(this.key); localStorage.removeItem(this.tokenKey);
    localStorage.removeItem('ydm.adminapp.fcm_token');
    this.currentUser.set(null);
  }
  login(credentials: { username: string; password: string }): Observable<LoginResponse> {
    return this.http.post<LoginResponse>(`${this.apiUrl}/login`, credentials).pipe(tap(s => this.acceptSession(s)));
  }
  refresh(): Observable<LoginResponse> {
    if (this.refreshing) return this.refreshing;
    const original = this.currentUser();
    const execute = async () => {
      this.readSession();
      const saved = this.currentUser();
      if (!saved) throw new Error('Session expired');
      if (saved.token !== original?.token && !this.isTokenExpired()) return saved;
      const replacement = await firstValueFrom(this.http.post<LoginResponse>(`${this.apiUrl}/refresh-token`, { token: saved.token, refreshToken: saved.refreshToken }));
      // Logout/account switching during refresh must not restore protected state.
      if (this.currentUser()?.userId !== saved.userId || this.currentUser()?.refreshToken !== saved.refreshToken) throw new Error('Account session changed');
      this.acceptSession(replacement);
      return replacement;
    };
    const operation: Promise<LoginResponse> = (async () => navigator.locks ? await navigator.locks.request('ydm.adminapp.refresh', execute) : await execute())();
    this.refreshing = from(operation).pipe(finalize(() => this.refreshing = undefined), shareReplay({ bufferSize: 1, refCount: false }));
    return this.refreshing;
  }
  logout(): void {
    const saved = this.currentUser();
    if (saved) this.revokeSession(saved.refreshToken,localStorage.getItem('ydm.adminapp.fcm_token'));
    this.clearSession();
    this.router.navigate(['/login']);
  }
  private readonly revocationKey='ydm.adminapp.pending-revocations';
  private flushingRevocations=false;
  private revokeSession(refreshToken:string,deviceToken:string|null):void{
    try{const queued=JSON.parse(localStorage.getItem(this.revocationKey) || '[]');const rows=Array.isArray(queued) ? queued.filter(r=>r && typeof r.refreshToken==='string' && r.createdAt>Date.now()-30*86400000) : [];if(!rows.some(r=>r.refreshToken===refreshToken))rows.push({refreshToken,deviceToken,createdAt:Date.now()});localStorage.setItem(this.revocationKey,JSON.stringify(rows.slice(-16)));}catch{localStorage.setItem(this.revocationKey,JSON.stringify([{refreshToken,deviceToken,createdAt:Date.now()}]));}
    this.flushRevocations();
  }
  private flushRevocations():void{
    if(this.flushingRevocations || !navigator.onLine)return;
    let rows:{refreshToken:string;deviceToken:string|null;createdAt:number}[];
    try{rows=JSON.parse(localStorage.getItem(this.revocationKey) || '[]');if(!Array.isArray(rows))throw new Error();rows=rows.filter(r=>r && typeof r.refreshToken==='string' && r.createdAt>Date.now()-30*86400000);}catch{localStorage.removeItem(this.revocationKey);return;}
    const row=rows[0];if(!row){localStorage.removeItem(this.revocationKey);return;}this.flushingRevocations=true;
    this.http.post(`${this.apiUrl}/logout`,row).subscribe({next:()=>{this.flushingRevocations=false;try{const current=JSON.parse(localStorage.getItem(this.revocationKey) || '[]');localStorage.setItem(this.revocationKey,JSON.stringify(current.filter((r:any)=>r.refreshToken!==row.refreshToken)));}catch{localStorage.removeItem(this.revocationKey);}this.flushRevocations();},error:()=>this.flushingRevocations=false});
  }
  navigateAfterAuthentication(): void {
    const s = this.currentUser();
    this.router.navigate([!s ? '/login' : s.requiresPasswordChange ? '/change-password' : s.requiresProfileCompletion ? '/complete-profile' : '/dashboard']);
  }
  setUserAfterPasswordChange(s: LoginResponse): void { this.acceptSession(s); }
  setUserAfterProfileCompletion(_token: string): void { /* updateProfile already applied the complete server response. */ }
  refreshCurrentUserFullName(_name: string): void { /* Server-confirmed name arrives with the replacement session. */ }
  requiresPasswordChange(): boolean { return !!this.currentUser()?.requiresPasswordChange; }
  requiresProfileCompletion(): boolean { return !!this.currentUser()?.requiresProfileCompletion; }
  changePassword(currentPassword: string, newPassword: string): Observable<LoginResponse> {
    return this.http.post<LoginResponse>(`${this.apiUrl}/change-password`, { currentPassword, newPassword }).pipe(tap(s => this.acceptSession(s)));
  }
  isLoggedIn(): boolean { return !!this.currentUser(); }
  hasRequiredRole(): boolean { return ['Admin', 'Priest', 'Manager'].includes(this.currentUser()?.role || ''); }
  isTokenExpired(): boolean {
    try { return JSON.parse(atob((this.currentUser()?.token || '').split('.')[1].replace(/-/g, '+').replace(/_/g, '/'))).exp * 1000 < Date.now() + 30000; }
    catch { return true; }
  }
  shouldRedirectToHome(): boolean { return this.isLoggedIn() && this.hasRequiredRole(); }
  getToken(): string | null { return this.currentUser()?.token ?? null; }
  getProfile(): Observable<ProfileDto> { return this.http.get<ProfileDto>(`${environment.apiUrl}/Profile`); }
  updateProfile(dto: { fullName?: string; email?: string; phone?: string; address?: string }): Observable<LoginResponse> {
    return this.http.put<LoginResponse>(`${environment.apiUrl}/Profile`, dto).pipe(tap(s => this.acceptSession(s)));
  }
  isSecretary(): boolean { return this.currentUser()?.role === 'Secretary'; }
  isManager(): boolean { return this.currentUser()?.role === 'Manager'; }
  canRecordStudentAttendance(): boolean { return this.isSecretary() || this.isManager(); }
}
