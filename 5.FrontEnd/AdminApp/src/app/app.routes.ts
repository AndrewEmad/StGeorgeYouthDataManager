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
import { ReportsPage } from './pages/reports/reports.page';
import { ServantFollowUpPage } from './pages/reports/servant-follow-up.page';
import { StudentDistributionPage } from './pages/reports/student-distribution.page';
import { authGuard } from './guards/auth.guard';

export const routes: Routes = [
  { path: 'login', component: LoginPage },
  { path: 'change-password', component: ChangePasswordPage, canActivate: [authGuard] },
  {
    path: 'dashboard',
    component: DashboardPage,
    canActivate: [authGuard],
    children: [
      { path: 'stats', component: StatsPage },
      { path: 'servants', component: ServantsPage },
      { path: 'servants/:id', component: ServantDetailPage },
      { path: 'students/:id', component: StudentDetailPage },
      { path: 'students', component: StudentsPage },
      { path: 'removal-requests', component: RemovalRequestsPage },
      {
        path: 'reports',
        component: ReportsPage,
        children: [
          { path: 'servant-follow-up', component: ServantFollowUpPage },
          { path: 'student-distribution', component: StudentDistributionPage },
          { path: '', redirectTo: 'servant-follow-up', pathMatch: 'full' }
        ]
      },
      { path: 'backups', component: BackupsPage },
      { path: '', redirectTo: 'stats', pathMatch: 'full' }
    ]
  },
  { path: '', redirectTo: '/dashboard', pathMatch: 'full' },
  { path: '**', redirectTo: '/dashboard' }
];
