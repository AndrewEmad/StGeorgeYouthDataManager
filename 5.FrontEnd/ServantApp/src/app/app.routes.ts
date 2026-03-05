import { Routes } from '@angular/router';
import { ServantLayoutComponent } from './layout/servant-layout/servant-layout.component';
import { LoginPage } from './pages/login/login.page';
import { ChangePasswordPage } from './pages/change-password/change-password.page';
import { CompleteProfilePage } from './pages/complete-profile/complete-profile.page';
import { DashboardPage } from './pages/dashboard/dashboard.page';
import { ProfilePage } from './pages/profile/profile.page';
import { StudentsPage } from './pages/students/students.page';
import { StudentDetailPage } from './pages/students/student-detail/student-detail.page';
import { RequestAddStudentPage } from './pages/request-add-student/request-add-student.page';
import { RemindersPage } from './pages/reminders/reminders.page';
import { AttendancePage } from './pages/attendance/attendance.page';
import { authGuard } from './core/guards/auth.guard';
import { redirectIfAuthGuard } from './core/guards/redirect-if-auth.guard';
import { secretaryGuard } from './core/guards/secretary.guard';

export const routes: Routes = [
  { path: 'login', component: LoginPage, canActivate: [redirectIfAuthGuard] },
  { path: 'change-password', component: ChangePasswordPage, canActivate: [authGuard] },
  { path: 'complete-profile', component: CompleteProfilePage, canActivate: [authGuard] },
  {
    path: '',
    component: ServantLayoutComponent,
    canActivate: [authGuard],
    children: [
      { path: 'dashboard', component: DashboardPage, data: { title: 'لوحة التحكم' } },
      { path: 'profile', component: ProfilePage, data: { title: 'الملف الشخصي' } },
      { path: 'reminders', component: RemindersPage, data: { title: 'تذكيرات المتابعة' } },
      { path: 'students', component: StudentsPage, data: { title: 'المخدومين' } },
      { path: 'request-add-student', component: RequestAddStudentPage, data: { title: 'طلب إضافة مخدوم' } },
      { path: 'attendance', component: AttendancePage, canActivate: [secretaryGuard], data: { title: 'تسجيل الحضور' } },
      { path: 'students/:id', component: StudentDetailPage, data: { title: 'تفاصيل المخدوم' } },
      { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
      { path: '**', redirectTo: 'dashboard' },
    ]
  },
  { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
  { path: '**', redirectTo: 'dashboard' },
];
