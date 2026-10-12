using ClosedXML.Excel;
using YouthDataManager.Domain.Entities;
using YouthDataManager.Domain.Enums;
using YouthDataManager.Shared.Service.Abstractions;

namespace YouthDataManager.Reports.Service.Implementations.Services;

public sealed class ExcelExportService
{
    private static byte[] Write(string name,string[] headers,IEnumerable<object?[]> rows,CancellationToken ct)
    {
        using var book=new XLWorkbook();var sheet=book.Worksheets.Add(name);sheet.RightToLeft=true;
        for(var i=0;i<headers.Length;i++){sheet.Cell(1,i+1).Value=headers[i];sheet.Cell(1,i+1).Style.Font.Bold=true;}
        var row=2;
        foreach(var values in rows)
        {
            ct.ThrowIfCancellationRequested();
            for(var i=0;i<values.Length;i++)
            {
                var cell=sheet.Cell(row,i+1);
                if(values[i] is DateTime date){cell.Value=date;cell.Style.DateFormat.Format=headers[i]=="تاريخ الميلاد" ? "yyyy-mm-dd" : "yyyy-mm-dd hh:mm";}
                else if(values[i] is int number)cell.Value=number;
                else {cell.Value=values[i]?.ToString() ?? "";cell.Style.NumberFormat.Format="@";}
            }
            row++;
        }
        sheet.SheetView.FreezeRows(1);sheet.Columns(1,headers.Length).Width=24;sheet.Column(1).Width=30;
        sheet.Range(1,1,Math.Max(1,row-1),headers.Length).SetAutoFilter();sheet.RangeUsed()!.Style.Alignment.WrapText=true;
        sheet.PageSetup.PagesWide=1;sheet.PageSetup.SetRowsToRepeatAtTop(1,1);
        ct.ThrowIfCancellationRequested();using var output=new MemoryStream();book.SaveAs(output);ct.ThrowIfCancellationRequested();return output.ToArray();
    }
    private static DateTime Cairo(DateTime date) => TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(date,DateTimeKind.Utc),BusinessTime.Zone);
    private static DateTime? Cairo(DateTime? date) => date.HasValue ? Cairo(date.Value) : null;
    private static string CallOutcome(CallStatus s) => s switch {CallStatus.Answered=>"تم الرد",CallStatus.NotAnswered=>"لم يرد",CallStatus.Busy=>"مشغول",CallStatus.SwitchedOff=>"مغلق",CallStatus.WrongNumber=>"رقم غير صحيح",_=>"غير معروف"};
    private static string VisitResult(VisitOutcome o) => o switch {VisitOutcome.Successful=>"زيارة ناجحة",VisitOutcome.NotAtHome=>"غير موجود",VisitOutcome.RefusedVisit=>"رفض الزيارة",VisitOutcome.Postponed=>"مؤجلة",_=>"غير معروف"};
    public byte[] ExportStudents(IEnumerable<Student> rows,CancellationToken ct=default) => Write("المخدومون",["الاسم الكامل","المنطقة","الكلية","السنة الدراسية","الهاتف","الهاتف الإضافي","العنوان","أب الاعتراف","الخادم المسؤول","تاريخ الميلاد","شهر الميلاد","الملاحظات","تاريخ الإضافة"],
        rows.Select(s=>new object?[]{s.FullName,s.Area,s.College,s.AcademicYear,s.Phone,s.SecondaryPhone,s.Address,s.ConfessionFather,s.Servant?.FullName,s.BirthDate,s.BirthDate?.Month ?? s.BirthMonth,s.Notes,Cairo(s.CreatedAt)}),ct);
    public byte[] ExportCalls(IEnumerable<CallLog> rows,CancellationToken ct=default) => Write("المكالمات",["التاريخ بتوقيت القاهرة","المخدوم","مسجل المكالمة","النتيجة","الملاحظات","المتابعة القادمة"],rows.Select(c=>new object?[]{Cairo(c.CallDate),c.Student.FullName,c.Servant.FullName,CallOutcome(c.CallStatus),c.Notes,Cairo(c.NextFollowUpDate)}),ct);
    public byte[] ExportVisits(IEnumerable<HomeVisit> rows,CancellationToken ct=default) => Write("الزيارات",["التاريخ بتوقيت القاهرة","المخدوم","مسجل الزيارة","النتيجة","الملاحظات","المرافقون","الزيارة القادمة"],rows.Select(v=>new object?[]{Cairo(v.VisitDate),v.Student.FullName,v.Servant.FullName,VisitResult(v.VisitOutcome),v.Notes,string.Join("، ",v.Participants.Where(p=>p.ServantId!=v.ServantId).GroupBy(p=>p.ServantId).Select(g=>g.First().Servant.FullName)),Cairo(v.NextVisitDate)}),ct);
}
