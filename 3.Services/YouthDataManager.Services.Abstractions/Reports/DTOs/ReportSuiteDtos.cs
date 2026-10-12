namespace YouthDataManager.Reports.Service.Abstractions.DTOs;

public sealed class ReportSuiteFilter
{
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
    public Guid? ServantId { get; set; }
    public string? Search { get; set; }
    public string? Area { get; set; }
    public string? College { get; set; }
    public string? AcademicYear { get; set; }
    public int? Gender { get; set; }
    public int? BirthMonth { get; set; }
    public bool? HasServant { get; set; }
    public string? Status { get; set; }
    public string? Type { get; set; }
    public string? Issue { get; set; }
    public int Days { get; set; } = 14;
    public int MinConsecutiveAbsences { get; set; } = 3;
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 25;
    public ReportRequestDto StudentFilter => new(DateFrom,DateTo,ServantId,Area,College,Search,AcademicYear,Gender,BirthMonth,HasServant);
}
public record ReportColumn(string Key,string Label,string Type = "text");
public record ReportTable(string Title,string Period,string Scope,IReadOnlyList<ReportColumn> Columns,IReadOnlyList<Dictionary<string,object?>> Rows,int TotalCount,int Page,int PageSize,Dictionary<string,decimal?> Totals);
