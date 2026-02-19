import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export interface CreateStudentRequest {
  fullName: string;
  phone: string;
  address?: string | null;
  area?: string | null;
  college?: string | null;
  academicYear?: string | null;
  confessionFather?: string | null;
  gender: number; // 0 Male, 1 Female
  servantId?: string | null;
  birthDate?: string | null; // ISO date string for API
}

@Injectable({
  providedIn: 'root'
})
export class StudentCommandsService {
  private apiUrl = `${environment.apiUrl}/StudentCommands`;

  constructor(private http: HttpClient) {}

  create(request: CreateStudentRequest): Observable<string> {
    const body = {
      fullName: request.fullName,
      phone: request.phone,
      address: request.address ?? null,
      area: request.area ?? null,
      college: request.college ?? null,
      academicYear: request.academicYear ?? null,
      confessionFather: request.confessionFather ?? null,
      gender: Number(request.gender) || 0,
      servantId: request.servantId ?? null,
      birthDate: request.birthDate || null
    };
    return this.http.post<string>(this.apiUrl, body);
  }

  assignToServant(studentId: string, servantId: string): Observable<any> {
    return this.http.post(`${this.apiUrl}/${studentId}/assign/${servantId}`, {});
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }

  private static readonly CSV_TEMPLATE =
  "الاسم الكامل,العنوان,المنطقة,رقم التليفون,تاريخ الميلاد,الكلية,السنة الدراسية,أب الاعتراف,النوع\n";

  downloadCsvTemplate(): void {
    const blob = new Blob(['\ufeff' + StudentCommandsService.CSV_TEMPLATE], { type: 'text/csv;charset=utf-8' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = 'students_template.csv';
    a.click();
    URL.revokeObjectURL(url);
  }

  /** Parse CSV text (Arabic or English headers) into array of CreateStudentRequest. */
  parseCsvToStudents(csvText: string): CreateStudentRequest[] {
    debugger
    const lines = csvText.split(/\r?\n/).filter(l => l.length > 0);
    if (lines.length < 2) return [];
    const header = this.parseCsvLine(lines[0]);
    const getCol = (row: string[], ...names: string[]) => {
      for (const name of names) {
        const i = header.findIndex(h => h.trim() === name);
        if (i >= 0 && i < row.length) return (row[i] ?? '').trim();
      }
      return '';
    };
    const result: CreateStudentRequest[] = [];
    for (let r = 1; r < lines.length; r++) {
      const row = this.parseCsvLine(lines[r]);
      if (row.every(c => !c.trim())) continue;
      const fullName = getCol(row, 'الاسم الكامل', 'FullName');
      const phone = getCol(row, 'رقم التليفون', 'Phone');
      if (!fullName || !phone) continue;
      const bd = getCol(row, 'تاريخ الميلاد', 'BirthDate');
      let birthDate: string | null = null;
      if (bd && !isNaN(Date.parse(bd))) birthDate = new Date(bd).toISOString().slice(0, 10);
      const genderStr = getCol(row, 'النوع', 'Gender');
      const gender = genderStr === '1' || genderStr === 'أنثى' ? 1 : 0;
      result.push({
        fullName,
        phone,
        address: getCol(row, 'العنوان', 'Address') || null,
        area: getCol(row, 'المنطقة', 'Area') || null,
        college: getCol(row, 'الكلية', 'College') || null,
        academicYear: getCol(row, 'السنة الدراسية', 'AcademicYear') || null,
        confessionFather: getCol(row, 'أب الاعتراف', 'ConfessionFather') || null,
        gender,
        servantId: null,
        birthDate
      });
    }
    return result;
  }

  private parseCsvLine(line: string): string[] {
    const out: string[] = [];
    let buf = '';
    let inQuotes = false;
    for (let i = 0; i < line.length; i++) {
      const c = line[i];
      if (c === '"') { inQuotes = !inQuotes; continue; }
      if (!inQuotes && c === ',') { out.push(buf.trim()); buf = ''; continue; }
      buf += c;
    }
    out.push(buf.trim());
    return out;
  }

  bulkImport(items: CreateStudentRequest[]): Observable<BulkImportResult> {
    const body = items.map(req => ({
      fullName: req.fullName,
      phone: req.phone,
      address: req.address ?? null,
      area: req.area ?? null,
      college: req.college ?? null,
      academicYear: req.academicYear ?? null,
      confessionFather: req.confessionFather ?? null,
      gender: Number(req.gender) || 0,
      servantId: null,
      birthDate: req.birthDate || null
    }));
    return this.http.post<BulkImportResult>(`${this.apiUrl}/bulk-import`, body);
  }
}

export interface BulkImportResult {
  created: number;
  errors: { row: number; message: string }[];
}
