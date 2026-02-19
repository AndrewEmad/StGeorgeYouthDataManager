import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

@Injectable({
  providedIn: 'root',
})
export class ReportsService {
  private apiUrl = `${environment.apiUrl}/Reports`;

  constructor(private http: HttpClient) {}

  getManagerDashboard(): Observable<any> {
    return this.http.get(`${this.apiUrl}/dashboard/manager`);
  }

  getServantPerformance(
    servantId: string,
  ): Observable<{
    servantId: string;
    fullName: string;
    assignedStudentsCount: number;
    callsThisWeek: number;
    visitsThisWeek: number;
  }> {
    return this.http.get(`${this.apiUrl}/servant/${servantId}/performance`) as any;
  }

  getServantStats(servantIds: string[]): Observable<{ servantId: string; assignedStudentsCount: number; lastCallDate: string | null; lastVisitDate: string | null }[]> {
    if (!servantIds.length) return this.http.get<any[]>(`${this.apiUrl}/servant-stats`);
    const params = servantIds.map((id) => `ids=${encodeURIComponent(id)}`).join('&');
    return this.http.get<any[]>(`${this.apiUrl}/servant-stats?${params}`);
  }

  exportStudents(filter: any): Observable<Blob> {
    return this.http.get(`${this.apiUrl}/export/students`, {
      params: filter,
      responseType: 'blob',
    });
  }

  exportCalls(filter: any): Observable<Blob> {
    return this.http.get(`${this.apiUrl}/export/calls`, {
      params: filter,
      responseType: 'blob',
    });
  }

  exportVisits(filter: any): Observable<Blob> {
    return this.http.get(`${this.apiUrl}/export/visits`, {
      params: filter,
      responseType: 'blob',
    });
  }

  getServantActivitySummary(params: { dateFrom?: string; dateTo?: string; servantId?: string; page?: number; pageSize?: number }): Observable<PagedReport<ServantActivitySummary>> {
    const q = new URLSearchParams();
    if (params.dateFrom) q.set('dateFrom', params.dateFrom);
    if (params.dateTo) q.set('dateTo', params.dateTo);
    if (params.servantId) q.set('servantId', params.servantId);
    if (params.page != null) q.set('page', String(params.page));
    if (params.pageSize != null) q.set('pageSize', String(params.pageSize));
    const query = q.toString();
    return this.http.get<PagedReport<ServantActivitySummary>>(`${this.apiUrl}/servant-activity${query ? '?' + query : ''}`);
  }

  getStudentsWithNoRecentContact(params: { days?: number; servantId?: string; page?: number; pageSize?: number }): Observable<PagedReport<StudentNoContact>> {
    const q = new URLSearchParams();
    if (params.days != null) q.set('days', String(params.days));
    if (params.servantId) q.set('servantId', params.servantId);
    if (params.page != null) q.set('page', String(params.page));
    if (params.pageSize != null) q.set('pageSize', String(params.pageSize));
    const query = q.toString();
    return this.http.get<PagedReport<StudentNoContact>>(`${this.apiUrl}/students-no-contact${query ? '?' + query : ''}`);
  }

  getStudentsByArea(params: { page?: number; pageSize?: number } = {}): Observable<PagedReport<StudentsByGroup>> {
    const q = new URLSearchParams();
    if (params.page != null) q.set('page', String(params.page));
    if (params.pageSize != null) q.set('pageSize', String(params.pageSize));
    const query = q.toString();
    return this.http.get<PagedReport<StudentsByGroup>>(`${this.apiUrl}/students-by-area${query ? '?' + query : ''}`);
  }

  getStudentsByAcademicYear(params: { page?: number; pageSize?: number } = {}): Observable<PagedReport<StudentsByGroup>> {
    const q = new URLSearchParams();
    if (params.page != null) q.set('page', String(params.page));
    if (params.pageSize != null) q.set('pageSize', String(params.pageSize));
    const query = q.toString();
    return this.http.get<PagedReport<StudentsByGroup>>(`${this.apiUrl}/students-by-academic-year${query ? '?' + query : ''}`);
  }
}

export interface PagedReport<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export interface ServantActivitySummary {
  servantId: string;
  fullName: string;
  assignedCount: number;
  callsInPeriod: number;
  visitsInPeriod: number;
}

export interface StudentNoContact {
  studentId: string;
  fullName: string;
  servantId: string | null;
  servantName: string | null;
  lastContactDate: string | null;
}

export interface StudentsByGroup {
  groupKey: string;
  count: number;
  studentIds: string[] | null;
}
