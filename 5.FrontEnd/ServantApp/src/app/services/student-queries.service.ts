import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class StudentQueriesService {
  private apiUrl = `${environment.apiUrl}/StudentQueries`;

  constructor(private http: HttpClient) {}

  getAll(): Observable<any[]> {
    return this.http.get<any[]>(this.apiUrl);
  }

  getById(id: string): Observable<any> {
    return this.http.get<any>(`${this.apiUrl}/${id}`);
  }

  getByServantId(servantId: string): Observable<any[]> {
    return this.http.get<any[]>(`${this.apiUrl}/servant/${servantId}`);
  }

  getPaged(params: {
    page: number;
    pageSize: number;
    search?: string;
    hasServant?: boolean | null;
  }): Observable<{ items: any[]; totalCount: number; page: number; pageSize: number }> {
    const httpParams: Record<string, string> = {
      page: String(params.page),
      pageSize: String(params.pageSize),
    };
    if (params.search) httpParams['search'] = params.search;
    if (params.hasServant === true) httpParams['hasServant'] = 'true';
    if (params.hasServant === false) httpParams['hasServant'] = 'false';
    return this.http.get<{ items: any[]; totalCount: number; page: number; pageSize: number }>(`${this.apiUrl}/paged`, { params: httpParams });
  }

  getUnassignedForServant(params: { page?: number; pageSize?: number } = {}): Observable<{ items: Array<{ student: any; hasPendingRequestByMe: boolean }>; totalCount: number; page: number; pageSize: number }> {
    const httpParams: Record<string, string> = {};
    if (params.page != null) httpParams['page'] = String(params.page);
    if (params.pageSize != null) httpParams['pageSize'] = String(params.pageSize);
    return this.http.get<{ items: Array<{ student: any; hasPendingRequestByMe: boolean }>; totalCount: number; page: number; pageSize: number }>(
      `${this.apiUrl}/unassigned-for-servant`,
      Object.keys(httpParams).length ? { params: httpParams } : {}
    );
  }
}
