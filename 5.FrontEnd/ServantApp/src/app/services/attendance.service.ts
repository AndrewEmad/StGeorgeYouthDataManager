import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class AttendanceService {
  private apiUrl = `${environment.apiUrl}/Attendance`;

  constructor(private http: HttpClient) {}

  recordStudentAttendance(date: string, studentIds: string[]): Observable<void> {
    return this.http.post<void>(`${this.apiUrl}/students`, {
      date,
      ids: studentIds
    });
  }
}
