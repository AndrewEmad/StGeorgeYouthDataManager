import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export interface StudentAdditionRequestDto {
  id: string;
  fullName: string;
  phone: string;
  secondaryPhone?: string;
  area?: string;
  college?: string;
  academicYear?: string;
  notes?: string;
  gender: number;
  requestedByUserId: string;
  requestedByUserName: string;
  requestedAt: string;
  status: string;
}

@Injectable({
  providedIn: 'root'
})
export class StudentAdditionRequestService {
  private apiUrl = `${environment.apiUrl}/StudentAdditionRequests`;

  constructor(private http: HttpClient) {}

  getPending(): Observable<StudentAdditionRequestDto[]> {
    return this.http.get<StudentAdditionRequestDto[]>(`${this.apiUrl}/pending`);
  }

  approve(id: string): Observable<void> {
    return this.http.post<void>(`${this.apiUrl}/${id}/approve`, {});
  }

  reject(id: string): Observable<void> {
    return this.http.post<void>(`${this.apiUrl}/${id}/reject`, {});
  }
}
