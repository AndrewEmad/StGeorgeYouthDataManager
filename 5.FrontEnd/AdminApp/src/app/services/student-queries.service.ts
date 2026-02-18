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

  getPaged(params: {
    page: number;
    pageSize: number;
    search?: string;
    area?: string;
    academicYear?: string;
    gender?: number | null;
    servantId?: string | null;
    hasServant?: boolean | null;
    sortBy?: string | null;
    sortDesc?: boolean | null;
  }): Observable<{ items: any[]; totalCount: number; page: number; pageSize: number }> {
    const httpParams: Record<string, string> = {
      page: String(params.page),
      pageSize: String(params.pageSize),
      sortBy: params.sortBy && params.sortBy.length > 0 ? params.sortBy : 'fullName',
      sortDesc: params.sortDesc === true ? 'true' : 'false'
    };
    if (params.search) httpParams['search'] = params.search;
    if (params.area) httpParams['area'] = params.area;
    if (params.academicYear) httpParams['academicYear'] = params.academicYear;
    if (params.gender != null && params.gender !== undefined) httpParams['gender'] = String(params.gender);
    if (params.servantId && params.servantId !== '_none') httpParams['servantId'] = params.servantId;
    if (params.hasServant === true) httpParams['hasServant'] = 'true';
    if (params.hasServant === false) httpParams['hasServant'] = 'false';
    return this.http.get<{ items: any[]; totalCount: number; page: number; pageSize: number }>(`${this.apiUrl}/paged`, { params: httpParams });
  }

  getById(id: string): Observable<any> {
    return this.http.get<any>(`${this.apiUrl}/${id}`);
  }

  getDistinctAreas(): Observable<string[]> {
    return this.http.get<string[]>(`${this.apiUrl}/areas`);
  }

  getDistinctAcademicYears(): Observable<string[]> {
    return this.http.get<string[]>(`${this.apiUrl}/academic-years`);
  }
}
