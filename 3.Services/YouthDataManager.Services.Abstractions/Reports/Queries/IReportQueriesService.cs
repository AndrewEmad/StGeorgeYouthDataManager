using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using YouthDataManager.Reports.Service.Abstractions.DTOs;
using YouthDataManager.Shared.Service.Abstractions;

namespace YouthDataManager.Reports.Service.Abstractions.Queries;

public interface IReportQueriesService
{
    Task<ServiceResult<ServantDashboardDto>> GetServantDashboard(Guid servantId);
    Task<ServiceResult<ManagerDashboardDto>> GetManagerDashboard();
    Task<ServiceResult<PagedReportResult<ServantPerformanceDto>>> GetServantPerformancesPaged(int page, int pageSize);
    Task<ServiceResult<ServantPerformanceDto>> GetServantPerformance(Guid servantId);
    Task<ServiceResult<IEnumerable<ServantStatsDto>>> GetServantStats(IEnumerable<Guid> servantIds);
    
    Task<ServiceResult<byte[]>> ExportStudentsExcel(ReportRequestDto filter);
    Task<ServiceResult<byte[]>> ExportCallsExcel(ReportRequestDto filter);
    Task<ServiceResult<byte[]>> ExportVisitsExcel(ReportRequestDto filter);

    Task<ServiceResult<PagedReportResult<ServantActivitySummaryDto>>> GetServantActivitySummary(DateTime? dateFrom, DateTime? dateTo, Guid? servantId, int page, int pageSize);
    Task<ServiceResult<PagedReportResult<StudentNoContactDto>>> GetStudentsWithNoRecentContact(int days, Guid? servantId, int page, int pageSize);
    Task<ServiceResult<PagedReportResult<StudentsByGroupDto>>> GetStudentsByArea(int page, int pageSize);
    Task<ServiceResult<PagedReportResult<StudentsByGroupDto>>> GetStudentsByAcademicYear(int page, int pageSize);
}
