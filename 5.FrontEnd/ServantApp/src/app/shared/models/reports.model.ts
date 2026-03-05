/**
 * Servant dashboard stats from Reports API.
 */
export interface ServantDashboardStats {
  totalStudents: number;
  callsThisWeek: number;
  visitsThisWeek: number;
  studentsNeedingFollowUp: number;
  studentsNotContacted: number;
}
