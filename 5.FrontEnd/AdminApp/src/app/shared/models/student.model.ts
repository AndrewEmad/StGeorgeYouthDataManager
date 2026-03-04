export interface StudentEditLogDto {
  id: string;
  studentId: string;
  updatedAt: string;
  updatedByUserId: string;
  updatedByUserName: string;
  details: string;
}

export interface CreateStudentRequest {
  fullName: string;
  phone: string;
  address?: string | null;
  area?: string | null;
  college?: string | null;
  academicYear?: string | null;
  confessionFather?: string | null;
  gender: number;
  servantId?: string | null;
  birthDate?: string | null;
}

export interface UpdateStudentRequest {
  id: string;
  fullName: string;
  phone: string;
  address?: string | null;
  area?: string | null;
  college?: string | null;
  academicYear?: string | null;
  confessionFather?: string | null;
  gender: number;
  servantId?: string | null;
  birthDate?: string | null;
}

export interface BulkImportResult {
  created: number;
  errors: { row: number; message: string }[];
}
