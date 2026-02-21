import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export interface CreateStudentAdditionRequestDto {
  fullName: string;
  phone: string;
  secondaryPhone?: string;
  address?: string;
  area?: string;
  college?: string;
  academicYear?: string;
  confessionFather?: string;
  notes?: string;
  gender: number;
  birthDate?: string;
}

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

  create(dto: CreateStudentAdditionRequestDto): Observable<{ id: string }> {
    return this.http.post<{ id: string }>(this.apiUrl, dto);
  }

  getMyRequests(): Observable<StudentAdditionRequestDto[]> {
    return this.http.get<StudentAdditionRequestDto[]>(`${this.apiUrl}/my`);
  }
}
