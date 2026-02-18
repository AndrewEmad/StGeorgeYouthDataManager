import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export interface CallLogDto {
  id: string;
  studentId: string;
  studentName: string;
  callDate: string;
  callStatus: number;
  notes: string;
  nextFollowUpDate: string | null;
}

export interface HomeVisitDto {
  id: string;
  studentId: string;
  studentName: string;
  visitDate: string;
  visitOutcome: number;
  notes: string;
  nextVisitDate: string | null;
}

@Injectable({ providedIn: 'root' })
export class FollowUpService {
  private apiUrl = `${environment.apiUrl}/FollowUp`;

  constructor(private http: HttpClient) {}

  getCallHistory(studentId: string): Observable<CallLogDto[]> {
    return this.http.get<CallLogDto[]>(`${this.apiUrl}/history/calls/${studentId}`);
  }

  getVisitHistory(studentId: string): Observable<HomeVisitDto[]> {
    return this.http.get<HomeVisitDto[]>(`${this.apiUrl}/history/visits/${studentId}`);
  }

  getServantCallHistory(servantId: string): Observable<CallLogDto[]> {
    return this.http.get<CallLogDto[]>(`${this.apiUrl}/history/calls/servant/${servantId}`);
  }

  getServantVisitHistory(servantId: string): Observable<HomeVisitDto[]> {
    return this.http.get<HomeVisitDto[]>(`${this.apiUrl}/history/visits/servant/${servantId}`);
  }
}
