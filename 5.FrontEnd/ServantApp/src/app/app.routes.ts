import { Routes } from '@angular/router';
import { LoginComponent } from './components/login/login';
import { ChangePasswordComponent } from './components/change-password/change-password';
import { ServantLayoutComponent } from './components/servant-layout/servant-layout';
import { DashboardComponent } from './components/dashboard/dashboard';
import { ProfileComponent } from './components/profile/profile';
import { StudentListComponent } from './components/student-list/student-list';
import { StudentDetailComponent } from './components/student-detail/student-detail';
import { RequestAddStudentComponent } from './components/request-add-student/request-add-student';
import { RemindersComponent } from './components/reminders/reminders';
import { authGuard } from './guards/auth.guard';
import { redirectIfAuthGuard } from './guards/redirect-if-auth.guard';

export const routes: Routes = [
  { path: 'login', component: LoginComponent, canActivate: [redirectIfAuthGuard] },
  { path: 'change-password', component: ChangePasswordComponent, canActivate: [authGuard] },
  {
    path: '',
    component: ServantLayoutComponent,
    canActivate: [authGuard],
    children: [
      { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
      { path: 'dashboard', component: DashboardComponent },
      { path: 'profile', component: ProfileComponent },
      { path: 'reminders', component: RemindersComponent },
      { path: 'students', component: StudentListComponent },
      { path: 'request-add-student', component: RequestAddStudentComponent },
      { path: 'students/:id', component: StudentDetailComponent },
      { path: '**', redirectTo: 'dashboard' },
    ]
  },
  { path: '**', redirectTo: 'dashboard' },
];
