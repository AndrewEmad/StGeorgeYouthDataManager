export interface ServantPerformance {
  servantId: string;
  fullName: string;
  assignedStudentsCount: number;
  callsThisWeek: number;
  visitsThisWeek: number;
}

export interface ServantActivitySummary {
  servantId: string;
  fullName: string;
  assignedCount: number;
  callsInPeriod: number;
  visitsInPeriod: number;
}

export interface StudentNoContact {
  studentId: string;
  fullName: string;
  servantId: string | null;
  servantName: string | null;
  lastContactDate: string | null;
}

export interface StudentsByGroup {
  groupKey: string;
  count: number;
  studentIds: string[] | null;
}

/** Servant (user) with stats for admin list; supports full-data sort. */
export interface ServantWithStats {
  id: string;
  userName: string;
  fullName: string;
  email: string;
  phone: string | null;
  role: string;
  isActive: boolean;
  createdAt: string;
  updatedAt: string | null;
  photoPath: string | null;
  address: string | null;
  assignedStudentsCount: number;
  lastCallDate: string | null;
  lastVisitDate: string | null;
}
