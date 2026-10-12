import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, map } from 'rxjs';
import { environment } from '../../environments/environment';
import { CreateStudentRequest, UpdateStudentRequest, BulkImportResult } from '../shared/models';
import { parseCsv, csvDocument } from '@ydm/shared/core/utils';

export interface CsvStudentRow { row: number; student: CreateStudentRequest; error?: string; candidates?: {studentId?:string;name:string;row?:number}[];duplicateReviewed?:boolean; }

@Injectable({ providedIn: 'root' })
export class StudentCommandsService {
  private apiUrl = `${environment.apiUrl}/StudentCommands`;
  constructor(private http: HttpClient) {}
  create(request: CreateStudentRequest): Observable<string> { return this.http.post<string>(this.apiUrl, {...request,gender:Number(request.gender)}); }
  update(request: UpdateStudentRequest,version?:string): Observable<void> { return this.http.put<void>(`${this.apiUrl}/${request.id}`, {...request,gender:Number(request.gender)},{headers:version ? {'If-Match':version} : {}}); }
  unassign(id:string):Observable<unknown>{return this.http.post(`${this.apiUrl}/${id}/unassign`,{});}
  assignToServant(studentId: string, servantId: string): Observable<unknown> { return this.http.post(`${this.apiUrl}/${studentId}/assign/${servantId}`, {}); }
  bulkAssign(ids: string[], servantId: string): Observable<{ outcomes: { studentId: string; success: boolean; error?: string }[] }> { return this.http.post<{ outcomes: { studentId: string; success: boolean; error?: string }[] }>(`${this.apiUrl}/bulk-assignment`, { ids, servantId }); }
  delete(id: string,reason:string='Student archival',version?:string): Observable<void> { return this.http.delete<void>(`${this.apiUrl}/${id}`,{headers:{'X-Audit-Reason':reason,...(version ? {'If-Match':version} : {})}}); }
  uploadPhoto(id: string, file: File): Observable<{ photoPath: string }> { const data = new FormData(); data.append('file', file); return this.http.post<{ photoPath: string }>(`${this.apiUrl}/${id}/photo`, data); }
  downloadCsvTemplate(): void {
    const headers = ['FullName','Phone','SecondaryPhone','BirthDate','BirthMonth','Address','Area','College','AcademicYear','ConfessionFather','Notes','Gender'];
    const blob = new Blob([csvDocument(headers, [])], { type: 'text/csv;charset=utf-8' });
    const url = URL.createObjectURL(blob); const a = document.createElement('a'); a.href = url; a.download = 'students_template.csv'; a.click(); URL.revokeObjectURL(url);
  }
  previewCsv(text: string): CsvStudentRow[] {
    const records = parseCsv(text); if (!records.length) return [];
    const headers = records[0].values.map(h => h.trim().replace(/^\uFEFF/, ''));
    const get = (values: string[], ...names: string[]) => values[headers.findIndex(h => names.includes(h))]?.trim() || '';
    return records.slice(1).filter(r => r.values.some(v => v.trim())).map(record => {
      const v = record.values;
      const fullName = get(v, 'FullName', 'الاسم الكامل'); const phone = get(v, 'Phone', 'رقم التليفون');
      let birthday = get(v, 'BirthDate', 'تاريخ الميلاد'); const monthText = get(v, 'BirthMonth', 'شهر الميلاد');
      const genderText = get(v, 'Gender', 'النوع'); const gender = genderText === 'أنثى' ? 1 : genderText === 'ذكر' || !genderText ? 0 : Number(genderText);
      let error: string | undefined;
      if (!fullName || !phone) error = 'الاسم ورقم الهاتف مطلوبان';
      if (birthday) {
        if (/^\d{2}\/\d{2}\/\d{4}$/.test(birthday)) { const [day, month, year] = birthday.split('/'); birthday = `${year}-${month}-${day}`; }
        const date = new Date(birthday + 'T00:00:00Z');
        if (!/^\d{4}-\d{2}-\d{2}$/.test(birthday) || !Number.isFinite(date.getTime()) || date.toISOString().slice(0, 10) !== birthday || date.getUTCFullYear() < 1900 || date > new Date()) error = 'تاريخ الميلاد غير صالح؛ استخدم YYYY-MM-DD أو DD/MM/YYYY';
      }
      const birthMonth = monthText ? Number(monthText) : null;
      if (birthMonth !== null && (!Number.isInteger(birthMonth) || birthMonth < 1 || birthMonth > 12)) error = 'شهر الميلاد يجب أن يكون من 1 إلى 12';
      if (gender !== 0 && gender !== 1) error = 'النوع يجب أن يكون 0 أو 1';
      if (record.values.length !== headers.length) error = 'عدد الحقول لا يطابق عناوين الملف';
      return { row: record.line, error, student: { fullName, phone, secondaryPhone: get(v, 'SecondaryPhone', 'الهاتف الإضافي') || null,
        birthDate: birthday || null, birthMonth, gender, address: get(v, 'Address', 'العنوان') || null, area: get(v, 'Area', 'المنطقة') || null,
        college: get(v, 'College', 'الكلية') || null, academicYear: get(v, 'AcademicYear', 'السنة الدراسية') || null,
        confessionFather: get(v, 'ConfessionFather', 'أب الاعتراف') || null, notes: get(v, 'Notes', 'ملاحظات') || null, servantId: null } };
    });
  }
  parseCsvToStudents(text: string): CreateStudentRequest[] { return this.previewCsv(text).map(r => r.student); }
  reviewImport(operationId:string,rows:CsvStudentRow[]):Observable<{row:number;candidates:{studentId?:string;name:string;row?:number}[];error?:string}[]>{return this.http.post<any[]>(`${this.apiUrl}/import-preview`,{operationId,rows:rows.map(r=>({row:r.row,student:r.student}))});}
  importRows(operationId: string, rows: CsvStudentRow[]): Observable<BulkImportResult> {
    const rejected = rows.filter(r => r.error).map(r => ({ row: r.row, message: r.error! }));
    return this.http.post<{ outcomes: { row: number; studentId?: string; error?: string }[] }>(`${this.apiUrl}/bulk-import-v2`, { operationId, rows: rows.filter(r => !r.error).map(r => ({ row: r.row, student: r.student,duplicateReviewed:r.duplicateReviewed || false })) }).pipe(map(r => ({ created: r.outcomes.filter(o => o.studentId && !o.error).length, errors: [...rejected, ...r.outcomes.filter(o => o.error).map(o => ({ row: o.row, message: o.error! }))] })));
  }
  bulkImport(items: CreateStudentRequest[]): Observable<BulkImportResult> { return this.importRows(crypto.randomUUID(), items.map((student, index) => ({ row: index + 2, student }))); }
}
