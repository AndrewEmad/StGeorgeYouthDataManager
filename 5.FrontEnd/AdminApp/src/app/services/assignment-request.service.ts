import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { StudentAssignmentRequestDto } from '../shared/models';

@Injectable({
  providedIn: 'root'
})
export class AssignmentRequestService {
  private apiUrl = `${environment.apiUrl}/StudentAssignmentRequests`;

  constructor(private http: HttpClient) {}

  getPending(): Observable<StudentAssignmentRequestDto[]> {
    return this.http.get<StudentAssignmentRequestDto[]>(`${this.apiUrl}/pending`);
  }

  approve(id: string): Observable<void> {
    return this.http.post<void>(`${this.apiUrl}/${id}/approve`, {});
  }

  reject(id: string): Observable<void> {
    return this.http.post<void>(`${this.apiUrl}/${id}/reject`, {});
  }
}
