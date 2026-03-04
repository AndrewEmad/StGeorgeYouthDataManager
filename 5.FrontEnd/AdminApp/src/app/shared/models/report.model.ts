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
