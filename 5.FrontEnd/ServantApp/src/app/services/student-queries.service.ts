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

  getById(id: string): Observable<any> {
    return this.http.get<any>(`${this.apiUrl}/${id}`);
  }

  getByServantId(servantId: string): Observable<any[]> {
    return this.http.get<any[]>(`${this.apiUrl}/servant/${servantId}`);
  }
}
