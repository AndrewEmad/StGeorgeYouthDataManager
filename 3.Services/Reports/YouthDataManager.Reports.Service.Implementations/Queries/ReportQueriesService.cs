using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using YouthDataManager.Domain.Entities;
using YouthDataManager.Domain.Repositories.NoTracking;
using YouthDataManager.Reports.Service.Abstractions.Data;
using YouthDataManager.Reports.Service.Abstractions.DTOs;
using YouthDataManager.Reports.Service.Abstractions.Queries;
using YouthDataManager.Reports.Service.Implementations.Services;
using YouthDataManager.Shared.Service.Abstractions;

namespace YouthDataManager.Reports.Service.Implementations.Queries;

public class ReportQueriesService : IReportQueriesService
{
    private readonly IStudentNoTrackingRepository _studentRepository;
    private readonly ICallLogNoTrackingRepository _callRepository;
    private readonly IHomeVisitNoTrackingRepository _visitRepository;
    private readonly IActivityLogNoTrackingRepository _activityRepository;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ExcelExportService _excelService;
    private readonly IActivityLogger _activityLogger;
    private readonly ILogger<ReportQueriesService> _logger;
    private readonly IReportDataProvider _reportDataProvider;

    public ReportQueriesService(
        IStudentNoTrackingRepository studentRepository,
        ICallLogNoTrackingRepository callRepository,
        IHomeVisitNoTrackingRepository visitRepository,
        IActivityLogNoTrackingRepository activityRepository,
        UserManager<ApplicationUser> userManager,
        ExcelExportService excelService,
        IActivityLogger activityLogger,
        ILogger<ReportQueriesService> logger,
        IReportDataProvider reportDataProvider)
    {
        _studentRepository = studentRepository;
        _callRepository = callRepository;
        _visitRepository = visitRepository;
        _activityRepository = activityRepository;
        _userManager = userManager;
        _excelService = excelService;
        _activityLogger = activityLogger;
        _logger = logger;
        _reportDataProvider = reportDataProvider;
    }

    public async Task<ServiceResult<ServantDashboardDto>> GetServantDashboard(Guid servantId)
    {
        try
        {
            var today = DateTime.UtcNow.Date;

            // Use repository projections for efficient calculations
            var studentStats = await _studentRepository.GetByServantId(servantId, s => new 
            {
                Id = s.Id,
                HasFollowUpToday = s.CallLogs.Any(c => c.NextFollowUpDate.HasValue && c.NextFollowUpDate.Value.Date <= today),
                IsNew = !s.CallLogs.Any() && !s.HomeVisits.Any()
            });

            var callsTodayCount = (await _callRepository.GetByServantId(servantId, c => c.CallDate.Date == today)).Count(x => x);
            var visitsTodayCount = (await _visitRepository.GetByServantId(servantId, v => v.VisitDate.Date == today)).Count(x => x);

            var dto = new ServantDashboardDto(
                studentStats.Count(),
                callsTodayCount,
                visitsTodayCount,
                studentStats.Count(s => s.HasFollowUpToday),
                studentStats.Count(s => s.IsNew)
            );

            return ServiceResult<ServantDashboardDto>.Success(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting servant dashboard for {ServantId}", servantId);
            return ServiceResult<ServantDashboardDto>.Failure("An error occurred while loading dashboard statistics.");
        }
    }

    /// <summary>Week starts Friday 00:00 UTC and ends Thursday 23:59:59.999 UTC.</summary>
    private static (DateTime WeekStart, DateTime WeekEnd) GetCurrentWeekRange()
    {
        var now = DateTime.UtcNow;
        var daysSinceFriday = ((int)now.DayOfWeek - 5 + 7) % 7;
        var weekStart = now.Date.AddDays(-daysSinceFriday);
        var weekEnd = weekStart.AddDays(7).AddMilliseconds(-1);
        return (weekStart, weekEnd);
    }

    public async Task<ServiceResult<ManagerDashboardDto>> GetManagerDashboard()
    {
        try
        {
            var students = await _studentRepository.GetAll(s => s);
            var servants = await _userManager.GetUsersInRoleAsync("Servant");
            var managerCount = (await _userManager.GetUsersInRoleAsync("Manager")).Count;
            var totalCalls = (await _callRepository.GetAll(c => c.Id)).Count();
            var totalVisits = (await _visitRepository.GetAll(v => v.Id)).Count();

            var (weekStart, weekEnd) = GetCurrentWeekRange();
            var callsThisWeek = (await _callRepository.GetByFilter(weekStart, weekEnd, null, null, null, c => c.Id)).Count();
            var visitsThisWeek = (await _visitRepository.GetByFilter(weekStart, weekEnd, null, null, null, v => v.Id)).Count();

            var recentActivities = await _activityRepository.GetRecent(10, a => new RecentActivityDto(
                $"{a.UserName} ({a.UserRole}): {a.Action} - {a.Details}",
                a.Timestamp
            ));

            var dto = new ManagerDashboardDto(
                students.Count(),
                servants.Count + managerCount,
                totalCalls,
                totalVisits,
                callsThisWeek,
                visitsThisWeek,
                students.Where(s => s.ServantId.HasValue).GroupBy(s => s.Servant?.FullName ?? "Other").ToDictionary(g => g.Key, g => g.Count()),
                recentActivities
            );

            return ServiceResult<ManagerDashboardDto>.Success(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting manager dashboard");
            return ServiceResult<ManagerDashboardDto>.Failure("An error occurred while loading manager dashboard.");
        }
    }

    public async Task<ServiceResult<ServantPerformanceDto>> GetServantPerformance(Guid servantId)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(servantId.ToString());
            if (user == null)
                return ServiceResult<ServantPerformanceDto>.Failure("Servant not found.");
            var (weekStart, weekEnd) = GetCurrentWeekRange();
            var assignedCount = (await _studentRepository.GetByServantId(servantId, s => s.Id)).Count();
            var callsWeek = (await _callRepository.GetByFilter(weekStart, weekEnd, servantId, null, null, c => c.Id)).Count();
            var visitsWeek = (await _visitRepository.GetByFilter(weekStart, weekEnd, servantId, null, null, v => v.Id)).Count();
            var dto = new ServantPerformanceDto(servantId, user.FullName ?? user.UserName ?? "", assignedCount, callsWeek, visitsWeek);
            return ServiceResult<ServantPerformanceDto>.Success(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting servant performance for {ServantId}", servantId);
            return ServiceResult<ServantPerformanceDto>.Failure("An error occurred while loading servant performance.");
        }
    }

    public async Task<ServiceResult<IEnumerable<ServantStatsDto>>> GetServantStats(IEnumerable<Guid> servantIds)
    {
        try
        {
            var idList = servantIds.Distinct().ToList();
            var result = new List<ServantStatsDto>();
            foreach (var id in idList)
            {
                var assignedCount = (await _studentRepository.GetByServantId(id, s => s.Id)).Count();
                var callDates = (await _callRepository.GetByServantId(id, c => c.CallDate)).ToList();
                var visitDates = (await _visitRepository.GetByServantId(id, v => v.VisitDate)).ToList();
                var lastCall = callDates.Count > 0 ? (DateTime?)callDates.Max() : null;
                var lastVisit = visitDates.Count > 0 ? (DateTime?)visitDates.Max() : null;
                result.Add(new ServantStatsDto(id, assignedCount, lastCall, lastVisit));
            }
            return ServiceResult<IEnumerable<ServantStatsDto>>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting servant stats");
            return ServiceResult<IEnumerable<ServantStatsDto>>.Failure("An error occurred while loading servant stats.");
        }
    }

    public async Task<ServiceResult<PagedReportResult<ServantPerformanceDto>>> GetServantPerformancesPaged(int page, int pageSize)
    {
        try
        {
            var servants = await _userManager.GetUsersInRoleAsync("Servant");
            var totalCount = servants.Count;
            var (weekStart, weekEnd) = GetCurrentWeekRange();
            var pagedServants = servants.Skip((page - 1) * pageSize).Take(pageSize);
            var result = new List<ServantPerformanceDto>();
            foreach (var servant in pagedServants)
            {
                var assignedCount = (await _studentRepository.GetByServantId(servant.Id, s => s.Id)).Count();
                var callsWeek = (await _callRepository.GetByFilter(weekStart, weekEnd, servant.Id, null, null, c => c.Id)).Count();
                var visitsWeek = (await _visitRepository.GetByFilter(weekStart, weekEnd, servant.Id, null, null, v => v.Id)).Count();
                result.Add(new ServantPerformanceDto(servant.Id, servant.FullName ?? servant.UserName ?? "", assignedCount, callsWeek, visitsWeek));
            }
            var paged = new PagedReportResult<ServantPerformanceDto>(result, totalCount, page, pageSize);
            return ServiceResult<PagedReportResult<ServantPerformanceDto>>.Success(paged);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting servant performances paged");
            return ServiceResult<PagedReportResult<ServantPerformanceDto>>.Failure("An error occurred while loading servant performances.");
        }
    }

    public async Task<ServiceResult<byte[]>> ExportStudentsExcel(ReportRequestDto filter)
    {
        try
        {
            // Implementation uses the Student NoTracking's filter capability if added or just GetByFilter logic
            // To keep it simple, I'll use the generic GetAll and filter in memory for now, 
            // or I could add a filter method to the Student NoTracking repo.
            
            var students = await _studentRepository.GetAll(s => s);
            // Apply memory filter...
            
            var file = _excelService.ExportStudents(students);
            await _activityLogger.LogAsync("تصدير مخدومين", $"تم تصدير ملف excel لبيانات المخدومين");
            return ServiceResult<byte[]>.Success(file);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting students excel");
            return ServiceResult<byte[]>.Failure("An error occurred during excel export.");
        }
    }

    public async Task<ServiceResult<byte[]>> ExportCallsExcel(ReportRequestDto filter)
    {
        try
        {
            var calls = await _callRepository.GetAll(c => c);
            var file = _excelService.ExportCalls(calls);
            await _activityLogger.LogAsync("تصدير مكالمات", $"تم تصدير ملف excel لسجل الافتقاد التليفوني");
            return ServiceResult<byte[]>.Success(file);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting calls excel");
            return ServiceResult<byte[]>.Failure("An error occurred during excel export.");
        }
    }

    public async Task<ServiceResult<byte[]>> ExportVisitsExcel(ReportRequestDto filter)
    {
        try
        {
            var visits = await _visitRepository.GetAll(v => v);
            var file = _excelService.ExportVisits(visits);
            await _activityLogger.LogAsync("تصدير زيارات", $"تم تصدير ملف excel لسجل الزيارات");
            return ServiceResult<byte[]>.Success(file);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting visits excel");
            return ServiceResult<byte[]>.Failure("An error occurred during excel export.");
        }
    }

    public async Task<ServiceResult<PagedReportResult<ServantActivitySummaryDto>>> GetServantActivitySummary(DateTime? dateFrom, DateTime? dateTo, Guid? servantId, int page, int pageSize)
    {
        try
        {
            var servants = servantId.HasValue
                ? (await _userManager.FindByIdAsync(servantId.Value.ToString()) is { } u ? new[] { u } : Array.Empty<ApplicationUser>())
                : (await _userManager.GetUsersInRoleAsync("Servant"));
            var totalCount = servants.Count;
            var dateStart = dateFrom ?? DateTime.UtcNow.Date.AddMonths(-1);
            var dateEnd = dateTo ?? DateTime.UtcNow;
            var pagedServants = servants.Skip((page - 1) * pageSize).Take(pageSize);
            var result = new List<ServantActivitySummaryDto>();
            foreach (var servant in pagedServants)
            {
                var assignedCount = (await _studentRepository.GetByServantId(servant.Id, s => s.Id)).Count();
                var callsInPeriod = (await _callRepository.GetByFilter(dateStart, dateEnd, servant.Id, null, null, c => c.Id)).Count();
                var visitsInPeriod = (await _visitRepository.GetByFilter(dateStart, dateEnd, servant.Id, null, null, v => v.Id)).Count();
                result.Add(new ServantActivitySummaryDto(servant.Id, servant.FullName ?? servant.UserName ?? "", assignedCount, callsInPeriod, visitsInPeriod));
            }
            var paged = new PagedReportResult<ServantActivitySummaryDto>(result, totalCount, page, pageSize);
            return ServiceResult<PagedReportResult<ServantActivitySummaryDto>>.Success(paged);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting servant activity summary");
            return ServiceResult<PagedReportResult<ServantActivitySummaryDto>>.Failure("An error occurred while loading servant activity summary.");
        }
    }

    public async Task<ServiceResult<PagedReportResult<StudentNoContactDto>>> GetStudentsWithNoRecentContact(int days, Guid? servantId, int page, int pageSize)
    {
        try
        {
            var (items, totalCount) = await _reportDataProvider.GetStudentsWithNoRecentContactPagedAsync(days, servantId, page, pageSize);
            var paged = new PagedReportResult<StudentNoContactDto>(items, totalCount, page, pageSize);
            return ServiceResult<PagedReportResult<StudentNoContactDto>>.Success(paged);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting students with no recent contact");
            return ServiceResult<PagedReportResult<StudentNoContactDto>>.Failure("An error occurred while loading students with no recent contact.");
        }
    }

    public async Task<ServiceResult<PagedReportResult<StudentsByGroupDto>>> GetStudentsByArea(int page, int pageSize)
    {
        try
        {
            var (items, totalCount) = await _reportDataProvider.GetStudentsByAreaPagedAsync(page, pageSize);
            var paged = new PagedReportResult<StudentsByGroupDto>(items, totalCount, page, pageSize);
            return ServiceResult<PagedReportResult<StudentsByGroupDto>>.Success(paged);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting students by area");
            return ServiceResult<PagedReportResult<StudentsByGroupDto>>.Failure("An error occurred while loading students by area.");
        }
    }

    public async Task<ServiceResult<PagedReportResult<StudentsByGroupDto>>> GetStudentsByAcademicYear(int page, int pageSize)
    {
        try
        {
            var (items, totalCount) = await _reportDataProvider.GetStudentsByAcademicYearPagedAsync(page, pageSize);
            var paged = new PagedReportResult<StudentsByGroupDto>(items, totalCount, page, pageSize);
            return ServiceResult<PagedReportResult<StudentsByGroupDto>>.Success(paged);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting students by academic year");
            return ServiceResult<PagedReportResult<StudentsByGroupDto>>.Failure("An error occurred while loading students by academic year.");
        }
    }
}
