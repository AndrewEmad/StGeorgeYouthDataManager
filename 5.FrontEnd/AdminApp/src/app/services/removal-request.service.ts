import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { StudentRemovalRequestDto, RemovalType } from '../shared/models';

@Injectable({
  providedIn: 'root'
})
export class RemovalRequestService {
  private apiUrl = `${environment.apiUrl}/StudentRemovalRequests`;

  constructor(private http: HttpClient) {}

  getPending(): Observable<StudentRemovalRequestDto[]> {
    return this.http.get<StudentRemovalRequestDto[]>(`${this.apiUrl}/pending`);
  }

  approve(id: string): Observable<void> {
    return this.http.post<void>(`${this.apiUrl}/${id}/approve`, {});
  }

  reject(id: string): Observable<void> {
    return this.http.post<void>(`${this.apiUrl}/${id}/reject`, {});
  }
}
