import { Routes } from '@angular/router';
import { LoginComponent } from './components/login/login';
import { ChangePasswordComponent } from './components/change-password/change-password';
import { DashboardComponent } from './components/dashboard/dashboard';
import { StudentListComponent } from './components/student-list/student-list';
import { StudentDetailComponent } from './components/student-detail/student-detail';
import { authGuard } from './guards/auth.guard';

export const routes: Routes = [
  { path: 'login', component: LoginComponent },
  { path: 'change-password', component: ChangePasswordComponent, canActivate: [authGuard] },
  { path: 'dashboard', component: DashboardComponent, canActivate: [authGuard] },
  { path: 'students', component: StudentListComponent, canActivate: [authGuard] },
  { path: 'students/:id', component: StudentDetailComponent, canActivate: [authGuard] },
  { path: '', redirectTo: '/dashboard', pathMatch: 'full' },
  { path: '**', redirectTo: '/dashboard' },
];
