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
    Task<ServiceResult<ServantPerformanceDto>> GetServantPerformance(Guid servantId);
    Task<ServiceResult<IEnumerable<ServantStatsDto>>> GetServantStats(IEnumerable<Guid> servantIds);
    
    Task<ServiceResult<byte[]>> ExportStudentsExcel(ReportRequestDto filter);
    Task<ServiceResult<byte[]>> ExportCallsExcel(ReportRequestDto filter);
    Task<ServiceResult<byte[]>> ExportVisitsExcel(ReportRequestDto filter);
}
