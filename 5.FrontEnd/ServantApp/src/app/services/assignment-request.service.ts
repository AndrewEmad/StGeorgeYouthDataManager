import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class AssignmentRequestService {
  private apiUrl = `${environment.apiUrl}/StudentAssignmentRequests`;

  constructor(private http: HttpClient) {}

  create(studentId: string): Observable<{ id: string }> {
    return this.http.post<{ id: string }>(this.apiUrl, { studentId });
  }
}
