import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class StudentCommandsService {
  private apiUrl = `${environment.apiUrl}/StudentCommands`;

  constructor(private http: HttpClient) {}

  create(student: any): Observable<any> {
    return this.http.post(this.apiUrl, student);
  }

  update(id: string, student: any): Observable<any> {
    return this.http.put(`${this.apiUrl}/${id}`, student);
  }

  delete(id: string): Observable<any> {
    return this.http.delete(`${this.apiUrl}/${id}`);
  }

  assignToServant(studentId: string, servantId: string): Observable<any> {
    return this.http.post(`${this.apiUrl}/${studentId}/assign/${servantId}`, {});
  }
}
