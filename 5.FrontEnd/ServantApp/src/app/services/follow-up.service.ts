import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class FollowUpService {
  private apiUrl = `${environment.apiUrl}/FollowUp`;

  constructor(private http: HttpClient) {}

  registerCall(data: any): Observable<any> {
    return this.http.post(`${this.apiUrl}/call`, data);
  }

  registerVisit(data: any): Observable<any> {
    return this.http.post(`${this.apiUrl}/visit`, data);
  }

  getCallHistory(studentId: string): Observable<any[]> {
    return this.http.get<any[]>(`${this.apiUrl}/history/calls/${studentId}`);
  }

  getVisitHistory(studentId: string): Observable<any[]> {
    return this.http.get<any[]>(`${this.apiUrl}/history/visits/${studentId}`);
  }

  getServants(): Observable<{ id: string; fullName: string }[]> {
    return this.http.get<{ id: string; fullName: string }[]>(`${this.apiUrl}/servants`);
  }

  updateVisit(visitId: string, data: {
    visitDate: string;
    visitOutcome: number;
    notes: string;
    nextVisitDate: string | null;
    participantServantIds: string[] | null;
  }): Observable<void> {
    return this.http.put<void>(`${this.apiUrl}/visit/${visitId}`, {
      visitDate: data.visitDate,
      visitOutcome: data.visitOutcome,
      notes: data.notes || '',
      nextVisitDate: data.nextVisitDate,
      participantServantIds: data.participantServantIds?.length ? data.participantServantIds : null
    });
  }
}
