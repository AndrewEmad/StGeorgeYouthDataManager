import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { StudentAdditionRequestDto } from '../shared/models';

@Injectable({
  providedIn: 'root'
})
export class StudentAdditionRequestService {
  private apiUrl = `${environment.apiUrl}/StudentAdditionRequests`;

  constructor(private http: HttpClient) {}

  getPending(): Observable<StudentAdditionRequestDto[]> {
    return this.http.get<StudentAdditionRequestDto[]>(`${this.apiUrl}/pending`);
  }

  approve(id: string, assignToRequestor: boolean = true): Observable<void> {
    return this.http.post<void>(`${this.apiUrl}/${id}/approve?assignToRequestor=${assignToRequestor}`, {});
  }

  reject(id: string): Observable<void> {
    return this.http.post<void>(`${this.apiUrl}/${id}/reject`, {});
  }
}
