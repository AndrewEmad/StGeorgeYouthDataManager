import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth.guard';
import { redirectIfAuthGuard } from './core/guards/redirect-if-auth.guard';

export const routes: Routes = [
  { path: 'login', loadComponent: () => import('./pages/login/login').then(m => m.LoginComponent), canActivate: [redirectIfAuthGuard] },
  { path: 'change-password', loadComponent: () => import('./pages/change-password/change-password.page').then(m => m.ChangePasswordPage), canActivate: [authGuard] },
  { path: 'complete-profile', loadComponent: () => import('./pages/complete-profile/complete-profile.page').then(m => m.CompleteProfilePage), canActivate: [authGuard] },
  {
    path: 'dashboard',
    loadComponent: () => import('./pages/dashboard/dashboard.page').then(m => m.DashboardPage),
    canActivate: [authGuard],
    children: [
      {path:'operations',loadComponent:()=>import('@ydm/shared/pages/operations').then(m=>m.OperationsPage),data:{title:'حالة التطبيق'}},
      {path:'archived-students',loadComponent:()=>import('@ydm/shared/pages/archives').then(m=>m.ArchivesPage),data:{title:'المخدومون المؤرشفون'}},
      {path:'activity-history',loadComponent:()=>import('@ydm/shared/pages/activity-history').then(m=>m.PersonalActivityPage),data:{title:'سجل النشاط'}},
      {path:'notifications',loadComponent:()=>import('@ydm/shared/pages/notifications').then(m=>m.NotificationsPage),data:{title:'الإشعارات والتذكيرات'}},
      {path:'request-history',loadComponent:()=>import('@ydm/shared/pages/requests').then(m=>m.RequestsPage),data:{title:'سجل الطلبات'}},
      {path:'tasks',loadComponent:()=>import('@ydm/shared/pages/tasks').then(m=>m.TasksPage),data:{title:'متابعة الافتقاد'}},
      {path:'report-suite',loadComponent:()=>import('@ydm/shared/pages/reports').then(m=>m.ReportSuitePage),data:{title:'التقارير'}},
      { path: 'stats', loadComponent: () => import('./pages/stats/stats.page').then(m => m.StatsPage), data: { title: 'الإحصائيات' } },
      { path: 'attendance', loadComponent: () => import('@ydm/shared/pages/meetings').then(m => m.MeetingsPage), data: { title: 'تسجيل الحضور' } },
      { path: 'servants', loadComponent: () => import('./pages/servants/servant-list/servant-list').then(m => m.ServantListComponent), data: { title: 'الخدام' } },
      { path: 'servants/:id', loadComponent: () => import('./pages/servants/servant-details/servant-detail.page').then(m => m.ServantDetailPage), data: { title: 'تفاصيل الخادم' } },
      { path: 'students/:id', loadComponent: () => import('@ydm/shared/pages/student-detail').then(m => m.StudentDetailPage), data: { title: 'تفاصيل المخدوم' } },
      { path: 'students', loadComponent: () => import('./pages/students/student-list-admin/student-list-admin').then(m => m.StudentListAdminComponent), data: { title: 'المخدومين' } },
      {
        path: 'removal-requests',
        loadComponent: () => import('@ydm/shared/pages/requests').then(m => m.RequestsPage),
        data: { title: 'طلبات الإزالة' , kind:'removal', review:true},
      },
      {
        path: 'assignment-requests',
        loadComponent: () => import('@ydm/shared/pages/requests').then(m => m.RequestsPage),
        data: { title: 'طلبات توزيع المخدومين' , kind:'assignment', review:true},
      },
      {
        path: 'student-addition-requests',
        loadComponent: () => import('@ydm/shared/pages/requests').then(m => m.RequestsPage),
        data: { title: 'طلبات إضافة مخدومين' , kind:'addition', review:true},
      },
      {
        path: 'reports',
        loadComponent: () => import('./pages/reports/reports.page').then(m => m.ReportsPage),
        data: { title: 'التقارير' },
        children: [
          {
            path: 'servant-follow-up',
            redirectTo: '/dashboard/reports',pathMatch:'full',
          },
          {
            path: 'student-distribution',
            loadComponent: () => import('./pages/reports/student-distribution.page').then(m => m.StudentDistributionPage),
            data: { title: 'توزيع المخدومين' },
          },
          {
            path: 'servant-performance',
            loadComponent: () => import('./pages/reports/servant-performance-report.page').then(m => m.ServantPerformanceReportPage),
            data: { title: 'مكالمات وزيارات هذا الأسبوع' },
          },
          {
            path: 'servant-activity',
            loadComponent: () => import('./pages/reports/servant-activity-report.page').then(m => m.ServantActivityReportPage),
            data: { title: 'نشاط الخادم' },
          },
          {
            path: 'students-no-contact',
            loadComponent: () => import('./pages/reports/students-no-contact-report.page').then(m => m.StudentsNoContactReportPage),
            data: { title: 'بدون اتصال' },
          },
          {
            path: 'students-by-area',
            loadComponent: () => import('./pages/reports/students-by-area-report.page').then(m => m.StudentsByAreaReportPage),
            data: { title: 'حسب المنطقة' },
          },
          {
            path: 'students-by-year',
            loadComponent: () => import('./pages/reports/students-by-year-report.page').then(m => m.StudentsByYearReportPage),
            data: { title: 'حسب السنة' },
          },
          {
            path: 'students-by-birth-month',
            loadComponent: () => import('./pages/reports/students-by-birth-month-report.page').then(m => m.StudentsByBirthMonthReportPage),
            data: { title: 'حسب شهر الميلاد' },
          },
          { path: '', loadComponent: () => import('./pages/reports/reports-catalog.page').then(m => m.ReportsCatalogPage),pathMatch:'full',data:{title:'التقارير'} },
        ],
      },
      { path:'backups',redirectTo:'data-exports',pathMatch:'full' },
      { path: 'data-exports', loadComponent: () => import('./pages/backups/backups.page').then(m => m.BackupsPage), data: { title: 'تصدير البيانات' } },
      { path: 'profile', loadComponent: () => import('@ydm/shared/pages/profile').then(m => m.ProfilePage), data: { title: 'الملف الشخصي' } },
      { path: '', redirectTo: 'stats', pathMatch: 'full' },
    ],
  },
  { path: '', redirectTo: '/dashboard', pathMatch: 'full' },
  { path: '**', redirectTo: '/dashboard' },
];
