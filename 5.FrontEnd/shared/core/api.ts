import { InjectionToken } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
export const YDM_API = new InjectionToken<string>('YDM API');
export interface SessionContext { userId: string; privileged: boolean; root: string;role?:string;app:string;namespace:string; }
export const YDM_SESSION = new InjectionToken<() => SessionContext>('YDM session');
export function apiError(error: HttpErrorResponse): string {
  if(!navigator.onLine || error.status===0)return 'تعذر تأكيد النتيجة بسبب الاتصال. احتفظنا بمدخلاتك؛ راجع الحالة عند عودة الاتصال قبل إعادة المحاولة.';
  if(error.status===403)return 'ليست لديك صلاحية لهذا الإجراء.';
  if(error.status===409)return 'تغيرت البيانات منذ فتحها. حدّث القائمة ثم راجع بياناتك وحاول مجددًا.';
  return typeof error.error==='string' ? error.error : error.error?.message || error.error?.errors?.[0]?.message || 'تعذر إتمام العملية. احتفظنا بمدخلاتك؛ حاول مجددًا.';
}
export function saveBlob(blob: Blob,name: string): void {
  const url=URL.createObjectURL(blob);const link=document.createElement('a');link.href=url;link.download=name;link.click();setTimeout(()=>URL.revokeObjectURL(url),1000);
}
export const labels: Record<string,string> = {callAnswered:'مكالمات مجابة',callNotAnswered:'مكالمات بلا رد',callBusy:'مكالمات مشغولة',callSwitchedOff:'مكالمات والهاتف مغلق',callWrongNumber:'أرقام خاطئة',visitSuccessful:'زيارات ناجحة',visitNotAtHome:'زيارات والمخدوم غير موجود',visitRefusedVisit:'زيارات مرفوضة',visitPostponed:'زيارات مؤجلة',Call:'مكالمة',Visit:'زيارة',Open:'مفتوحة',Completed:'مكتملة',Cancelled:'ملغاة',PendingReview:'تحتاج مراجعة',Draft:'مسودة',Finalized:'معتمد',Pending:'قيد الانتظار',Approved:'مقبول',Rejected:'مرفوض',addition:'إضافة',assignment:'إسناد',removal:'إزالة',total:'الإجمالي',roster:'المخدومون المطابقون',attempted:'مخدومين جرت محاولة التواصل معهم',reached:'مخدومين تم التواصل معهم بنجاح',coverage:'التغطية %',neverAttempted:'لم تجرِ محاولة',noRecentAttempt:'بلا محاولة حديثة',noRecentSuccess:'بلا تواصل ناجح حديث',open:'مهام مفتوحة',completed:'مهام مكتملة',cancelled:'مهام ملغاة',pendingReview:'تحتاج مراجعة',dueStudents:'مخدومين لديهم مهام مستحقة',unassigned:'مخدومين غير مسندين',inactiveOwners:'مسؤولون غير نشطين',owners:'مسؤولون',pending:'طلبات معلقة',stalePending:'طلبات معلقة قديمة',averageTurnaroundHours:'متوسط المعالجة (ساعة)',eligible:'فرص حضور مؤكدة',present:'حضور',rate:'نسبة الحضور %'};
export function display(value: unknown,type='text'): string {
  if(value===null || value===undefined || value==='')return '—';
  if(type==='boolean')return value ? 'نعم' : 'لا';
  if(type==='approval-status')return ['قيد الانتظار','مقبول','مرفوض','ملغى'][Number(value)] || 'غير معروف';
  if(type==='datetime'){const text=String(value);const d=new Date(/(Z|[+-]\d{2}:?\d{2})$/.test(text) ? text : text+'Z');return Number.isFinite(d.getTime()) ? new Intl.DateTimeFormat('ar-EG',{dateStyle:'medium',timeStyle:'short',timeZone:'Africa/Cairo'}).format(d) : '—';}
  if(type==='date')return String(value).slice(0,10);
  return labels[String(value)] || String(value);
}

export interface LoginSession {token:string;refreshToken:string;userId:string;fullName:string;role:string;requiresPasswordChange?:boolean;requiresProfileCompletion?:boolean;}
export const YDM_AUTH=new InjectionToken<{acceptSession(session:LoginSession):void;navigateAfterAuthentication():void}>('YDM account operations');
