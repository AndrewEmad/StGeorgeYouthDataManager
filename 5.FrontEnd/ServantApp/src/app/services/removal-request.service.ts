import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export interface StudentRemovalRequestDto {
  id: string;
  studentId: string;
  studentName: string;
  requestedByUserId: string;
  requestedByUserName: string;
  requestedAt: string;
  status: string;
  processedAt?: string;
  processedByUserId?: string;
}

@Injectable({
  providedIn: 'root'
})
export class RemovalRequestService {
  private apiUrl = `${environment.apiUrl}/StudentRemovalRequests`;

  constructor(private http: HttpClient) {}

  create(studentId: string): Observable<{ id: string }> {
    return this.http.post<{ id: string }>(this.apiUrl, { studentId });
  }

  getMyRequests(): Observable<StudentRemovalRequestDto[]> {
    return this.http.get<StudentRemovalRequestDto[]>(`${this.apiUrl}/my`);
  }

  getPendingForStudent(studentId: string): Observable<StudentRemovalRequestDto | null> {
    return this.http.get<StudentRemovalRequestDto | null>(`${this.apiUrl}/student/${studentId}`);
  }
}
