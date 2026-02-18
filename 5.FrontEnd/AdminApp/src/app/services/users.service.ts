import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export interface User {
  id: string;
  userName: string;
  fullName: string;
  email: string;
  phone: string;
  role: string;
  isActive: boolean;
  createdAt: string;
}

@Injectable({
  providedIn: 'root'
})
export class UsersService {
  private apiUrl = `${environment.apiUrl}/Users`;

  constructor(private http: HttpClient) {}

  getAll(): Observable<User[]> {
    return this.http.get<User[]>(this.apiUrl);
  }

  getPaged(params: { page: number; pageSize: number; search?: string; role?: string; isActive?: boolean | null }): Observable<{ items: User[]; totalCount: number; page: number; pageSize: number }> {
    let url = `${this.apiUrl}/paged?page=${params.page}&pageSize=${params.pageSize}`;
    if (params.search) url += `&search=${encodeURIComponent(params.search)}`;
    if (params.role) url += `&role=${encodeURIComponent(params.role)}`;
    if (params.isActive === true) url += `&isActive=true`;
    if (params.isActive === false) url += `&isActive=false`;
    return this.http.get<{ items: User[]; totalCount: number; page: number; pageSize: number }>(url);
  }

  getById(id: string): Observable<User> {
    return this.http.get<User>(`${this.apiUrl}/${id}`);
  }

  create(user: any): Observable<string> {
    return this.http.post<string>(this.apiUrl, user);
  }

  update(id: string, user: any): Observable<void> {
    return this.http.put<void>(`${this.apiUrl}/${id}`, user);
  }

  toggleStatus(id: string): Observable<void> {
    return this.http.post<void>(`${this.apiUrl}/${id}/toggle-status`, {});
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }
}
