import { Routes } from '@angular/router';
import { LoginPage } from './pages/login/login.page';
import { ChangePasswordPage } from './pages/change-password/change-password.page';
import { DashboardPage } from './pages/dashboard/dashboard.page';
import { StatsPage } from './pages/stats/stats.page';
import { ServantsPage } from './pages/servants/servants.page';
import { ServantDetailPage } from './pages/servants/servant-detail.page';
import { StudentsPage } from './pages/students/students.page';
import { StudentDetailPage } from './pages/students/student-detail.page';
import { BackupsPage } from './pages/backups/backups.page';
import { RemovalRequestsPage } from './pages/removal-requests/removal-requests.page';
import { AssignmentRequestsPage } from './pages/assignment-requests/assignment-requests.page';
import { StudentAdditionRequestsPage } from './pages/student-addition-requests/student-addition-requests.page';
import { ReportsPage } from './pages/reports/reports.page';
import { ServantFollowUpPage } from './pages/reports/servant-follow-up.page';
import { ServantActivityReportPage } from './pages/reports/servant-activity-report.page';
import { StudentsNoContactReportPage } from './pages/reports/students-no-contact-report.page';
import { StudentsByAreaReportPage } from './pages/reports/students-by-area-report.page';
import { StudentsByYearReportPage } from './pages/reports/students-by-year-report.page';
import { ServantPerformanceReportPage } from './pages/reports/servant-performance-report.page';
import { AttendancePage } from './pages/attendance/attendance.page';
import { ProfilePage } from './pages/profile/profile.page';
import { authGuard } from './guards/auth.guard';
import { redirectIfAuthGuard } from './guards/redirect-if-auth.guard';

export const routes: Routes = [
  { path: 'login', component: LoginPage, canActivate: [redirectIfAuthGuard] },
  { path: 'change-password', component: ChangePasswordPage, canActivate: [authGuard] },
  {
    path: 'dashboard',
    component: DashboardPage,
    canActivate: [authGuard],
    children: [
      { path: 'stats', component: StatsPage },
      { path: 'attendance', component: AttendancePage },
      { path: 'servants', component: ServantsPage },
      { path: 'servants/:id', component: ServantDetailPage },
      { path: 'students/:id', component: StudentDetailPage },
      { path: 'students', component: StudentsPage },
      { path: 'removal-requests', component: RemovalRequestsPage },
      { path: 'assignment-requests', component: AssignmentRequestsPage },
      { path: 'student-addition-requests', component: StudentAdditionRequestsPage },
      {
        path: 'reports',
        component: ReportsPage,
        children: [
          { path: 'servant-follow-up', component: ServantFollowUpPage },
          { path: 'student-distribution', redirectTo: 'servant-follow-up', pathMatch: 'full' },
          { path: 'servant-performance', component: ServantPerformanceReportPage },
          { path: 'servant-activity', component: ServantActivityReportPage },
          { path: 'students-no-contact', component: StudentsNoContactReportPage },
          { path: 'students-by-area', component: StudentsByAreaReportPage },
          { path: 'students-by-year', component: StudentsByYearReportPage },
          { path: '', redirectTo: 'servant-follow-up', pathMatch: 'full' }
        ]
      },
      { path: 'backups', component: BackupsPage },
      { path: 'profile', component: ProfilePage },
      { path: '', redirectTo: 'stats', pathMatch: 'full' }
    ]
  },
  { path: '', redirectTo: '/dashboard', pathMatch: 'full' },
  { path: '**', redirectTo: '/dashboard' }
];
