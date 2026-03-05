import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { PagedResult } from '../shared/models';
import { ServantPerformance, ServantActivitySummary, StudentNoContact, StudentsByGroup, ServantWithStats } from '../shared/models/report.model';

@Injectable({
  providedIn: 'root',
})
export class ReportsService {
  private apiUrl = `${environment.apiUrl}/Reports`;

  constructor(private http: HttpClient) {}

  getManagerDashboard(): Observable<any> {
    return this.http.get(`${this.apiUrl}/dashboard/manager`);
  }

  getServantPerformancesPaged(params: { page: number; pageSize: number; sortBy?: string; sortDesc?: boolean }): Observable<PagedResult<ServantPerformance>> {
    const p: Record<string, string> = { page: String(params.page), pageSize: String(params.pageSize) };
    if (params.sortBy) p['sortBy'] = params.sortBy;
    if (params.sortDesc != null) p['sortDesc'] = String(params.sortDesc);
    return this.http.get<PagedResult<ServantPerformance>>(`${this.apiUrl}/servant-performances-paged`, { params: p });
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

  getServantsPagedWithStats(params: {
    page: number;
    pageSize: number;
    search?: string;
    role?: string;
    isActive?: boolean | null;
    sortBy?: string | null;
    sortDesc?: boolean;
  }): Observable<PagedResult<ServantWithStats>> {
    const q = new URLSearchParams();
    q.set('page', String(params.page));
    q.set('pageSize', String(params.pageSize));
    if (params.search) q.set('search', params.search);
    if (params.role) q.set('role', params.role);
    if (params.isActive !== undefined && params.isActive !== null) q.set('isActive', String(params.isActive));
    if (params.sortBy) q.set('sortBy', params.sortBy);
    if (params.sortDesc != null) q.set('sortDesc', String(params.sortDesc));
    return this.http.get<PagedResult<ServantWithStats>>(`${this.apiUrl}/servants-paged-with-stats?${q.toString()}`);
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

  getServantActivitySummary(params: { dateFrom?: string; dateTo?: string; servantId?: string; page?: number; pageSize?: number; sortBy?: string; sortDesc?: boolean }): Observable<PagedResult<ServantActivitySummary>> {
    const q = new URLSearchParams();
    if (params.dateFrom) q.set('dateFrom', params.dateFrom);
    if (params.dateTo) q.set('dateTo', params.dateTo);
    if (params.servantId) q.set('servantId', params.servantId);
    if (params.page != null) q.set('page', String(params.page));
    if (params.pageSize != null) q.set('pageSize', String(params.pageSize));
    if (params.sortBy) q.set('sortBy', params.sortBy);
    if (params.sortDesc != null) q.set('sortDesc', String(params.sortDesc));
    const query = q.toString();
    return this.http.get<PagedResult<ServantActivitySummary>>(`${this.apiUrl}/servant-activity${query ? '?' + query : ''}`);
  }

  getStudentsWithNoRecentContact(params: { days?: number; servantId?: string; page?: number; pageSize?: number; sortBy?: string; sortDesc?: boolean }): Observable<PagedResult<StudentNoContact>> {
    const q = new URLSearchParams();
    if (params.days != null) q.set('days', String(params.days));
    if (params.servantId) q.set('servantId', params.servantId);
    if (params.page != null) q.set('page', String(params.page));
    if (params.pageSize != null) q.set('pageSize', String(params.pageSize));
    if (params.sortBy) q.set('sortBy', params.sortBy);
    if (params.sortDesc != null) q.set('sortDesc', String(params.sortDesc));
    const query = q.toString();
    return this.http.get<PagedResult<StudentNoContact>>(`${this.apiUrl}/students-no-contact${query ? '?' + query : ''}`);
  }

  getStudentsByArea(params: { page?: number; pageSize?: number; sortBy?: string; sortDesc?: boolean } = {}): Observable<PagedResult<StudentsByGroup>> {
    const q = new URLSearchParams();
    if (params.page != null) q.set('page', String(params.page));
    if (params.pageSize != null) q.set('pageSize', String(params.pageSize));
    if (params.sortBy) q.set('sortBy', params.sortBy);
    if (params.sortDesc != null) q.set('sortDesc', String(params.sortDesc));
    const query = q.toString();
    return this.http.get<PagedResult<StudentsByGroup>>(`${this.apiUrl}/students-by-area${query ? '?' + query : ''}`);
  }

  getStudentsByAcademicYear(params: { page?: number; pageSize?: number; sortBy?: string; sortDesc?: boolean } = {}): Observable<PagedResult<StudentsByGroup>> {
    const q = new URLSearchParams();
    if (params.page != null) q.set('page', String(params.page));
    if (params.pageSize != null) q.set('pageSize', String(params.pageSize));
    if (params.sortBy) q.set('sortBy', params.sortBy);
    if (params.sortDesc != null) q.set('sortDesc', String(params.sortDesc));
    const query = q.toString();
    return this.http.get<PagedResult<StudentsByGroup>>(`${this.apiUrl}/students-by-academic-year${query ? '?' + query : ''}`);
  }

  getStudentsByBirthMonth(params: { page?: number; pageSize?: number; sortBy?: string; sortDesc?: boolean } = {}): Observable<PagedResult<StudentsByGroup>> {
    const q = new URLSearchParams();
    if (params.page != null) q.set('page', String(params.page));
    if (params.pageSize != null) q.set('pageSize', String(params.pageSize));
    if (params.sortBy) q.set('sortBy', params.sortBy);
    if (params.sortDesc != null) q.set('sortDesc', String(params.sortDesc));
    const query = q.toString();
    return this.http.get<PagedResult<StudentsByGroup>>(`${this.apiUrl}/students-by-birth-month${query ? '?' + query : ''}`);
  }
}
