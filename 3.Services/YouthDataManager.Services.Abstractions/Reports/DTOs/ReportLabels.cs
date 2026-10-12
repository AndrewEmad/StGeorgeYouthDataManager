namespace YouthDataManager.Reports.Service.Abstractions.DTOs;

public static class ReportLabels
{
    public static string Arabic(string value)=>value switch {
        "Call"=>"مكالمة","Visit"=>"زيارة","Open" or "open"=>"مهام مفتوحة","Completed" or "completed"=>"مهام مكتملة","Cancelled" or "cancelled"=>"ملغاة","PendingReview" or "pendingReview"=>"تحتاج مراجعة","Pending" or "pending"=>"قيد الانتظار","Approved"=>"مقبول","Rejected"=>"مرفوض",
        "total"=>"الإجمالي","roster"=>"المخدومون المطابقون","attempted"=>"مخدومين جرت محاولة التواصل معهم","reached"=>"مخدومين تم التواصل معهم بنجاح","coverage"=>"نسبة التغطية","neverAttempted" or "never-attempted"=>"لم تجرِ محاولة","noRecentAttempt" or "no-recent-attempt"=>"بلا محاولة حديثة","noRecentSuccess" or "no-recent-success"=>"بلا تواصل ناجح حديث",
        "dueStudents"=>"مخدومين لديهم مهام مستحقة","unassigned"=>"غير مسندين","inactiveOwners" or "inactive-owner"=>"مسؤولون غير نشطين","owners"=>"المسؤولون","stalePending"=>"طلبات معلقة قديمة","averageTurnaroundHours"=>"متوسط المعالجة بالساعات",
        "eligible"=>"فرص حضور مؤكدة","present"=>"الحضور","rate"=>"نسبة الحضور","phone"=>"هاتف ناقص أو غير صالح","address"=>"عنوان ناقص","area"=>"منطقة ناقصة","year"=>"سنة دراسية ناقصة","birthday"=>"ميلاد ناقص أو غير صالح","duplicates"=>"تشابه محتمل",
        "unassigned-group"=>"بلا خادم وقت الاجتماع","consecutive-absence"=>"غياب متتالي","attendance-unknown"=>"بيانات حضور غير كافية","overdue-tasks"=>"متابعة متأخرة",
        "calls"=>"المكالمات","visits"=>"الزيارات الفعلية","visitCredits"=>"مشاركات الخدام في الزيارات","openTasks"=>"متابعات مفتوحة الآن","overdueTasks"=>"متابعات متأخرة الآن","absent"=>"مرات الغياب","servantEligible"=>"فرص حضور الخدام","servantPresent"=>"حضور الخدام","servantRate"=>"نسبة حضور الخدام %","consecutiveAbsenceStudents"=>"مخدومون لديهم غياب متتالي","unknownAttendanceStudents"=>"بيانات حضور غير كافية","overdueStudents"=>"مخدومون لديهم متابعة متأخرة",
        "callAnswered"=>"مكالمات مجابة","callNotAnswered"=>"مكالمات بلا رد","callBusy"=>"مكالمات مشغولة","callSwitchedOff"=>"مكالمات والهاتف مغلق","callWrongNumber"=>"أرقام خاطئة","visitSuccessful"=>"زيارات ناجحة","visitNotAtHome"=>"المخدوم غير موجود","visitRefusedVisit"=>"زيارات مرفوضة","visitPostponed"=>"زيارات مؤجلة",_=>value
    };
}
