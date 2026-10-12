using YouthDataManager.Reports.Service.Abstractions.Data;
using YouthDataManager.Reports.Service.Abstractions.DTOs;
using YouthDataManager.Reports.Service.Abstractions.Queries;
using YouthDataManager.Reports.Service.Implementations.Services;
using YouthDataManager.Shared.Service.Abstractions;

namespace YouthDataManager.Reports.Service.Implementations.Queries;

public sealed class ReportQueriesService(IReportDataProvider data,ExcelExportService excel,ICallerContext caller) : IReportQueriesService
{
    public async Task<ServiceResult<ServantDashboardDto>> GetServantDashboard(Guid id) => ServiceResult<ServantDashboardDto>.Success(await data.ServantDashboard(id));
    public async Task<ServiceResult<ManagerDashboardDto>> GetManagerDashboard() => ServiceResult<ManagerDashboardDto>.Success(await data.ManagerDashboard());
    public async Task<ServiceResult<PagedReportResult<ServantPerformanceDto>>> GetServantPerformancesPaged(int page,int size,string? sortBy=null,bool? sortDesc=null) => ServiceResult<PagedReportResult<ServantPerformanceDto>>.Success(await data.Performances(page,size,sortBy,sortDesc==true));
    public async Task<ServiceResult<ServantPerformanceDto>> GetServantPerformance(Guid id)
    {
        var result=await data.Performances(1,1,null,false,id);
        return result.Items.Count==0 ? ServiceResult<ServantPerformanceDto>.Failure("Servant not found.") : ServiceResult<ServantPerformanceDto>.Success(result.Items[0]);
    }
    public async Task<ServiceResult<IEnumerable<ServantStatsDto>>> GetServantStats(IEnumerable<Guid> ids) => ServiceResult<IEnumerable<ServantStatsDto>>.Success(await data.Stats(ids));
    public async Task<ServiceResult<PagedReportResult<ServantWithStatsDto>>> GetServantsPagedWithStats(int page,int size,string? search=null,string? role=null,bool? isActive=null,string? sortBy=null,bool? sortDesc=null) => ServiceResult<PagedReportResult<ServantWithStatsDto>>.Success(await data.Servants(page,size,search,role,isActive,sortBy,sortDesc==true));
    public async Task<ServiceResult<byte[]>> ExportStudentsExcel(ReportRequestDto filter) => ServiceResult<byte[]>.Success(excel.ExportStudents(await data.ExportStudents(filter),caller.CancellationToken));
    public async Task<ServiceResult<byte[]>> ExportCallsExcel(ReportRequestDto filter) => ServiceResult<byte[]>.Success(excel.ExportCalls(await data.ExportCalls(filter),caller.CancellationToken));
    public async Task<ServiceResult<byte[]>> ExportVisitsExcel(ReportRequestDto filter) => ServiceResult<byte[]>.Success(excel.ExportVisits(await data.ExportVisits(filter),caller.CancellationToken));
    public async Task<ServiceResult<PagedReportResult<ServantActivitySummaryDto>>> GetServantActivitySummary(DateTime? from,DateTime? to,Guid? id,int page,int size,string? sortBy=null,bool? sortDesc=null) => ServiceResult<PagedReportResult<ServantActivitySummaryDto>>.Success(await data.Activity(from,to,id,page,size,sortBy,sortDesc==true));
    public async Task<ServiceResult<PagedReportResult<StudentNoContactDto>>> GetStudentsWithNoRecentContact(int days,Guid? id,int page,int size,string? sortBy=null,bool? sortDesc=null)
    {
        var (items,total)=await data.GetStudentsWithNoRecentContactPagedAsync(days,id,page,size,sortBy,sortDesc==true);
        return ServiceResult<PagedReportResult<StudentNoContactDto>>.Success(new(items,total,page,size));
    }
    public async Task<ServiceResult<PagedReportResult<StudentsByGroupDto>>> GetStudentsByArea(int page,int size,string? sortBy=null,bool? sortDesc=null)
    {
        var (items,total)=await data.GetStudentsByAreaPagedAsync(page,size,sortBy,sortDesc==true);return ServiceResult<PagedReportResult<StudentsByGroupDto>>.Success(new(items,total,page,size));
    }
    public async Task<ServiceResult<PagedReportResult<StudentsByGroupDto>>> GetStudentsByAcademicYear(int page,int size,string? sortBy=null,bool? sortDesc=null)
    {
        var (items,total)=await data.GetStudentsByAcademicYearPagedAsync(page,size,sortBy,sortDesc==true);return ServiceResult<PagedReportResult<StudentsByGroupDto>>.Success(new(items,total,page,size));
    }
    public async Task<ServiceResult<PagedReportResult<StudentsByGroupDto>>> GetStudentsByBirthMonth(int page,int size,string? sortBy=null,bool? sortDesc=null)
    {
        var (items,total)=await data.GetStudentsByBirthMonthPagedAsync(page,size,sortBy,sortDesc==true);return ServiceResult<PagedReportResult<StudentsByGroupDto>>.Success(new(items,total,page,size));
    }
}
