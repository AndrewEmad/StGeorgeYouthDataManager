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
}
