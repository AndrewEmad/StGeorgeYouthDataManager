using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using YouthDataManager.Reports.Service.Abstractions.DTOs;

namespace YouthDataManager.Reports.Service.Abstractions.Data;

public interface IReportDataProvider
{
    Task<ReportTable> Suite(string report, ReportSuiteFilter filter, bool export = false);
    Task<ServantDashboardDto> ServantDashboard(Guid id);
    Task<ManagerDashboardDto> ManagerDashboard();
    Task<PagedReportResult<ServantPerformanceDto>> Performances(int page,int size,string? sort,bool desc,Guid? id=null);
    Task<IReadOnlyList<ServantStatsDto>> Stats(IEnumerable<Guid> ids);
    Task<PagedReportResult<ServantWithStatsDto>> Servants(int page,int size,string? search,string? role,bool? active,string? sort,bool desc);
    Task<PagedReportResult<ServantActivitySummaryDto>> Activity(DateTime? from,DateTime? to,Guid? id,int page,int size,string? sort,bool desc);
    Task<IReadOnlyList<YouthDataManager.Domain.Entities.Student>> ExportStudents(ReportRequestDto filter);
    Task<IReadOnlyList<YouthDataManager.Domain.Entities.CallLog>> ExportCalls(ReportRequestDto filter);
    Task<IReadOnlyList<YouthDataManager.Domain.Entities.HomeVisit>> ExportVisits(ReportRequestDto filter);
    Task<(IReadOnlyList<StudentNoContactDto> Items, int TotalCount)> GetStudentsWithNoRecentContactPagedAsync(int days, Guid? servantId, int page, int pageSize, string? sortBy = null, bool sortDesc = false);
    Task<(IReadOnlyList<StudentsByGroupDto> Items, int TotalCount)> GetStudentsByAreaPagedAsync(int page, int pageSize, string? sortBy = null, bool sortDesc = false);
    Task<(IReadOnlyList<StudentsByGroupDto> Items, int TotalCount)> GetStudentsByAcademicYearPagedAsync(int page, int pageSize, string? sortBy = null, bool sortDesc = false);
    Task<(IReadOnlyList<StudentsByGroupDto> Items, int TotalCount)> GetStudentsByBirthMonthPagedAsync(int page, int pageSize, string? sortBy = null, bool sortDesc = false);
}
