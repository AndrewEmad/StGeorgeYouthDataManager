import {requestsRedirect} from '@ydm/shared/core/route-aliases';
import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth.guard';
import { redirectIfAuthGuard } from './core/guards/redirect-if-auth.guard';
import { secretaryGuard } from './core/guards/secretary.guard';

export const routes: Routes = [
  { path: 'login', loadComponent: () => import('./pages/login/login.page').then(m => m.LoginPage), canActivate: [redirectIfAuthGuard] },
  { path: 'change-password', loadComponent: () => import('./pages/change-password/change-password.page').then(m => m.ChangePasswordPage), canActivate: [authGuard] },
  { path: 'complete-profile', loadComponent: () => import('./pages/complete-profile/complete-profile.page').then(m => m.CompleteProfilePage), canActivate: [authGuard] },
  {
    path: '',
    loadComponent: () => import('./layout/servant-layout/servant-layout.component').then(m => m.ServantLayoutComponent),
    canActivate: [authGuard],
    children: [
      {path:'help',loadComponent:()=>import('@ydm/shared/pages/help').then(m=>m.HelpPage),data:{title:'دليل الاستخدام'}},
      {path:'activity-history',loadComponent:()=>import('@ydm/shared/pages/activity-history').then(m=>m.PersonalActivityPage),data:{title:'سجل النشاط'}},
      {path:'notifications',loadComponent:()=>import('@ydm/shared/pages/notifications').then(m=>m.NotificationsPage),data:{title:'الإشعارات والتذكيرات'}},
      {path:'requests',loadComponent:()=>import('@ydm/shared/pages/requests').then(m=>m.RequestsPage),data:{title:'طلباتي'}},
      {path:'request-history',redirectTo:requestsRedirect('',{kind:'all',status:'',mine:'true'}),pathMatch:'full'},
      {path:'tasks',loadComponent:()=>import('@ydm/shared/pages/tasks').then(m=>m.TasksPage),data:{title:'متابعاتي'}},
      {path:'report-suite',loadComponent:()=>import('@ydm/shared/pages/reports').then(m=>m.ReportSuitePage),data:{title:'تقارير مجموعتي وحضوري'}},
      { path: 'dashboard', loadComponent: () => import('./pages/dashboard/dashboard.page').then(m => m.DashboardPage), data: { title: 'خدمتي' } },
      { path: 'profile', loadComponent: () => import('@ydm/shared/pages/profile').then(m => m.ProfilePage), data: { title: 'الملف الشخصي' } },
      { path: 'reminders', redirectTo:'notifications',pathMatch:'full', data: { title: 'تذكيرات المتابعة' } },
      { path: 'students', loadComponent: () => import('./pages/students/students.page').then(m => m.StudentsPage), data: { title: 'مجموعتي' } },
      { path: 'request-add-student', loadComponent: () => import('./pages/request-add-student/request-add-student.page').then(m => m.RequestAddStudentPage), data: { title: 'طلب إضافة مخدوم' } },
      { path: 'attendance', loadComponent: () => import('@ydm/shared/pages/meetings').then(m => m.MeetingsPage), canActivate: [secretaryGuard], data: { title: 'تسجيل الحضور' } },
      { path: 'students/:id', loadComponent: () => import('@ydm/shared/pages/student-detail').then(m => m.StudentDetailPage), data: { title: 'تفاصيل المخدوم' } },
      { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
      { path: '**', redirectTo: 'dashboard' },
    ]
  },
  { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
  { path: '**', redirectTo: 'dashboard' },
];
