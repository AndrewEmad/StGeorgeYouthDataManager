using System.Globalization;
using System.Text;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YouthDataManager.Reports.Service.Abstractions.Data;
using YouthDataManager.Reports.Service.Abstractions.DTOs;
using YouthDataManager.Shared.Service.Abstractions;

namespace YouthDataManager.WebApi.Controllers;

[ApiController,Route("api/reports/suite"),Authorize]
public sealed class ReportSuiteController(IReportDataProvider provider,YouthDataManager.WebApi.Authorization.ResourceAccess access) : ControllerBase
{
    private bool Denied(string report,ReportSuiteFilter filter) => !access.Privileged &&
        (report is "servant-service-summary" or "group-attendance" or "pastoral-follow-up" || filter.ServantId.HasValue && filter.ServantId!=access.UserId);
    [HttpGet("{report}")]
    public async Task<IActionResult> Read(string report,[FromQuery] ReportSuiteFilter filter,bool full=false) => Denied(report,filter) ? Forbid() : Ok(await provider.Suite(report,filter,full));
    [HttpGet("{report}/export")]
    public async Task<IActionResult> Export(string report,[FromQuery] ReportSuiteFilter filter,string format="xlsx")
    {
        if(Denied(report,filter))return Forbid();
        if(format is not ("xlsx" or "csv"))return BadRequest("Invalid export format.");
        var table=await provider.Suite(report,filter,true);var ct=HttpContext.RequestAborted;
        if(format=="csv")
        {
            static string Cell(object? value)
            {
                var text=Convert.ToString(value,CultureInfo.InvariantCulture) ?? "";
                if(text.Length>0 && "=+-@".Contains(text.TrimStart().FirstOrDefault()))text="'"+text;
                return "\""+text.Replace("\"","\"\"")+"\"";
            }
            var content=new StringBuilder("\uFEFF").AppendJoin(",",table.Columns.Select(c=>Cell(c.Label))).Append("\r\n");
            foreach(var row in table.Rows){ct.ThrowIfCancellationRequested();content.AppendJoin(",",table.Columns.Select(c=>Cell(Value(row[c.Key],c.Type) is DateTime d ? d.ToString(c.Type=="date" ? "yyyy-MM-dd" : "yyyy-MM-dd HH:mm:ss",CultureInfo.InvariantCulture) : Value(row[c.Key],c.Type)))).Append("\r\n");}
            return File(Encoding.UTF8.GetBytes(content.ToString()),"text/csv; charset=utf-8",report+".csv");
        }
        using var workbook=new XLWorkbook();var sheet=workbook.AddWorksheet("التقرير");sheet.RightToLeft=true;
        for(var i=0;i<table.Columns.Count;i++){sheet.Cell(1,i+1).Value=table.Columns[i].Label;sheet.Column(i+1).Width=24;}
        for(var i=0;i<table.Rows.Count;i++)
        {
            ct.ThrowIfCancellationRequested();
            for(var j=0;j<table.Columns.Count;j++)
            {
                var column=table.Columns[j];var cell=sheet.Cell(i+2,j+1);var value=Value(table.Rows[i][column.Key],column.Type);
                if(value is DateTime date){cell.Value=date;cell.Style.DateFormat.Format=column.Type=="date" ? "yyyy-mm-dd" : "yyyy-mm-dd hh:mm";}
                else if(value is decimal number)cell.Value=number;
                else if(value is int integer)cell.Value=integer;
                else{cell.Style.NumberFormat.Format="@";cell.Value=Convert.ToString(value,CultureInfo.InvariantCulture) ?? "—";}
            }
        }
        sheet.Row(1).Style.Font.Bold=true;sheet.SheetView.FreezeRows(1);sheet.Style.Alignment.WrapText=true;sheet.PageSetup.SetRowsToRepeatAtTop(1,1);
        sheet.PageSetup.PageOrientation=XLPageOrientation.Landscape;sheet.PageSetup.FitToPages(1,0);
        var metadata=workbook.AddWorksheet("نطاق التقرير");metadata.RightToLeft=true;metadata.Column(1).Width=30;metadata.Column(2).Width=100;metadata.Style.Alignment.WrapText=true;
        metadata.Cell(1,1).Value="التقرير";metadata.Cell(1,2).Value=table.Title;metadata.Cell(2,1).Value="الفترة / القاهرة";metadata.Cell(2,2).Value=table.Period;metadata.Cell(3,1).Value="الفلاتر ونطاق الصفوف";metadata.Cell(3,2).Value=table.Scope;metadata.Cell(4,1).Value="عدد النتائج الكاملة";metadata.Cell(4,2).Value=table.TotalCount;
        var totalRow=6;foreach(var total in table.Totals){ct.ThrowIfCancellationRequested();metadata.Cell(totalRow,1).Value=ReportLabels.Arabic(total.Key);if(total.Value.HasValue)metadata.Cell(totalRow,2).Value=total.Value.Value;else metadata.Cell(totalRow,2).Value="—";totalRow++;}
        ct.ThrowIfCancellationRequested();using var stream=new MemoryStream();workbook.SaveAs(stream);ct.ThrowIfCancellationRequested();return File(stream.ToArray(),"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",report+".xlsx");
    }
    private static object? Value(object? value,string type)
    {
        if(value is DateTime date)return type=="datetime" ? TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(date,DateTimeKind.Utc),BusinessTime.Zone) : date;
        if(value is bool boolean)return boolean ? "نعم" : "لا";
        if(type=="approval-status" && value is int status)return status switch {0=>"قيد الانتظار",1=>"مقبول",2=>"مرفوض",3=>"ملغى",_=>"غير معروف"};
        if(value is string text)return text switch {"Call"=>"مكالمة","Visit"=>"زيارة","Open"=>"مفتوحة","Completed"=>"مكتملة","Cancelled"=>"ملغاة","PendingReview"=>"تحتاج مراجعة","Finalized"=>"معتمد","Draft"=>"مسودة","addition"=>"إضافة","assignment"=>"إسناد","removal"=>"إزالة",_=>text};
        return value;
    }
}
