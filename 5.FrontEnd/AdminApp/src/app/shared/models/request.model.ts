export interface StudentAssignmentRequestDto {
  id: string;
  studentId: string;
  studentName: string;
  requestedByUserId: string;
  requestedByUserName: string;
  requestedAt: string;
  status: string;
  processedAt?: string;
  processedByUserId?: string;
}

export const RemovalType = { UnassignOnly: 0, DeleteFromSystem: 1 } as const;

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
