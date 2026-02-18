using System;
using System.Collections.Generic;
using System.IO;
using ClosedXML.Excel;
using YouthDataManager.Domain.Entities;

namespace YouthDataManager.Reports.Service.Implementations.Services;

public class ExcelExportService
{
    public byte[] ExportStudents(IEnumerable<Student> students)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("الطلاب");
        worksheet.RightToLeft = true;

        var headers = new[] { "الاسم بالكامل", "المنطقة", "الكلية", "السنة الدراسية", "رقم التليفون", "أب الاعتراف", "الخادم المسئول" };
        for (int i = 0; i < headers.Length; i++)
        {
            worksheet.Cell(1, i + 1).Value = headers[i];
            worksheet.Cell(1, i + 1).Style.Font.Bold = true;
        }

        int row = 2;
        foreach (var student in students)
        {
            worksheet.Cell(row, 1).Value = student.FullName;
            worksheet.Cell(row, 2).Value = student.Area;
            worksheet.Cell(row, 3).Value = student.College;
            worksheet.Cell(row, 4).Value = student.AcademicYear;
            worksheet.Cell(row, 5).Value = student.Phone;
            worksheet.Cell(row, 6).Value = student.ConfessionFather;
            worksheet.Cell(row, 7).Value = student.Servant?.FullName ?? "غير محدد";
            row++;
        }

        worksheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public byte[] ExportCalls(IEnumerable<CallLog> calls)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("الافتقاد التليفوني");
        worksheet.RightToLeft = true;

        var headers = new[] { "التاريخ", "المخدوم", "الخادم", "المناسبة", "النتيجة", "تاريخ المتابعة" };
        for (int i = 0; i < headers.Length; i++)
        {
            worksheet.Cell(1, i + 1).Value = headers[i];
            worksheet.Cell(1, i + 1).Style.Font.Bold = true;
        }

        int row = 2;
        foreach (var call in calls)
        {
            worksheet.Cell(row, 1).Value = call.CallDate.ToShortDateString();
            worksheet.Cell(row, 2).Value = call.Student?.FullName ?? "غير محدد";
            worksheet.Cell(row, 3).Value = call.Servant?.FullName ?? "غير محدد";
            worksheet.Cell(row, 4).Value = call.CallStatus.ToString();
            worksheet.Cell(row, 5).Value = call.Notes;
            worksheet.Cell(row, 6).Value = call.NextFollowUpDate?.ToShortDateString() ?? "-";
            row++;
        }

        worksheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public byte[] ExportVisits(IEnumerable<HomeVisit> visits)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("الزيارات");
        worksheet.RightToLeft = true;

        var headers = new[] { "التاريخ", "المخدوم", "الخادم", "المناسبة", "الملاحظات" };
        for (int i = 0; i < headers.Length; i++)
        {
            worksheet.Cell(1, i + 1).Value = headers[i];
            worksheet.Cell(1, i + 1).Style.Font.Bold = true;
        }

        int row = 2;
        foreach (var visit in visits)
        {
            worksheet.Cell(row, 1).Value = visit.VisitDate.ToShortDateString();
            worksheet.Cell(row, 2).Value = visit.Student?.FullName ?? "غير محدد";
            worksheet.Cell(row, 3).Value = visit.Servant?.FullName ?? "غير محدد";
            worksheet.Cell(row, 4).Value = visit.VisitOutcome.ToString();
            worksheet.Cell(row, 5).Value = visit.Notes;
            row++;
        }

        worksheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }
}
