import { Routes } from '@angular/router';
import { LoginComponent } from './components/login/login';
import { ChangePasswordComponent } from './components/change-password/change-password';
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
  { path: 'dashboard', component: DashboardComponent, canActivate: [authGuard] },
  { path: 'profile', component: ProfileComponent, canActivate: [authGuard] },
  { path: 'reminders', component: RemindersComponent, canActivate: [authGuard] },
  { path: 'students', component: StudentListComponent, canActivate: [authGuard] },
  { path: 'request-add-student', component: RequestAddStudentComponent, canActivate: [authGuard] },
  { path: 'students/:id', component: StudentDetailComponent, canActivate: [authGuard] },
  { path: '', redirectTo: '/dashboard', pathMatch: 'full' },
  { path: '**', redirectTo: '/dashboard' },
];
