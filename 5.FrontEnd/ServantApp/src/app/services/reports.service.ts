import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class ReportsService {
  private apiUrl = `${environment.apiUrl}/Reports`;

  constructor(private http: HttpClient) {}

  getServantDashboard(): Observable<any> {
    return this.http.get(`${this.apiUrl}/dashboard/servant`);
  }

  getManagerDashboard(): Observable<any> {
    return this.http.get(`${this.apiUrl}/dashboard/manager`);
  }

  exportStudents(filter: any): Observable<Blob> {
    return this.http.get(`${this.apiUrl}/export/students`, {
      params: filter,
      responseType: 'blob'
    });
  }
}
