/**
 * Student entity as returned by StudentQueries (getById, list items).
 */
export interface Student {
  id: string;
  fullName: string;
  phone: string;
  secondaryPhone?: string;
  address?: string;
  area?: string;
  college?: string;
  academicYear?: string;
  confessionFather?: string;
  gender: Gender;
  birthDate?: string;
  photoPath?: string;
  servantId?: string;
  notes?: string;
  lastAttendanceDate?: string;
  createdAt?: string;
  updatedAt?: string;
}

/**
 * Gender enum (0 = Male, 1 = Female).
 */
export type Gender = 0 | 1;

/** Options for gender select (Arabic labels). */
export const GENDER_OPTIONS: { value: number; label: string }[] = [
  { value: 0, label: 'ذكر' },
  { value: 1, label: 'أنثى' },
];

/**
 * Item in servant student list (student + segment: Assigned, Unassigned, Requested).
 */
export interface ServantStudentItem {
  student: Student;
  segment: string;
}

/**
 * Params for paged student list for servant.
 */
export interface StudentPagedParams {
  page: number;
  pageSize: number;
  search?: string;
  area?: string;
  academicYear?: string;
  gender?: number;
}

/**
 * Payload for creating a student (admin/commands).
 */
export interface CreateStudentDto {
  fullName: string;
  phone: string;
  secondaryPhone?: string;
  address?: string;
  area?: string;
  college?: string;
  academicYear?: string;
  confessionFather?: string;
  gender: Gender;
  birthDate?: string;
  notes?: string;
}

/**
 * Payload for updating a student.
 */
export interface UpdateStudentDto {
  id: string;
  fullName: string;
  phone: string;
  address?: string;
  area?: string;
  college?: string;
  academicYear?: string;
  confessionFather?: string;
  gender: Gender;
  birthDate?: string | null;
  servantId?: string;
}

/**
 * Student removal request (from RemovalRequestService).
 */
export interface StudentRemovalRequestDto {
  id: string;
  studentId: string;
  studentName: string;
  requestedByUserId: string;
  requestedByUserName: string;
  requestedAt: string;
  status: string;
  removalType: number;
  processedAt?: string;
  processedByUserId?: string;
}

/**
 * Removal type for student removal request.
 */
export const RemovalType = { UnassignOnly: 0, DeleteFromSystem: 1 } as const;
export type RemovalType = (typeof RemovalType)[keyof typeof RemovalType];

/**
 * Payload for creating a student addition request (servant requests to add a student).
 */
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

/**
 * Student addition request from API.
 */
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
