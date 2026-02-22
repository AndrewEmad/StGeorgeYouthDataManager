using System;
using System.Collections.Generic;

namespace YouthDataManager.Reports.Service.Abstractions.DTOs;

public record ServantDashboardDto(
    int TotalStudents,
    int CallsThisWeek,
    int VisitsThisWeek,
    int StudentsNeedingFollowUp,
    int StudentsNotContacted
);

public record ManagerDashboardDto(
    int TotalStudents,
    int TotalServants,
    int TotalCalls,
    int TotalVisits,
    int CallsThisWeek,
    int VisitsThisWeek,
    Dictionary<string, int> StudentsPerServant,
    IEnumerable<RecentActivityDto> RecentActivities
);

public record ServantPerformanceDto(
    Guid ServantId,
    string FullName,
    int AssignedStudentsCount,
    int CallsThisWeek,
    int VisitsThisWeek
);

public record ServantStatsDto(
    Guid ServantId,
    int AssignedStudentsCount,
    DateTime? LastCallDate,
    DateTime? LastVisitDate
);

public record RecentActivityDto(
    string Description,
    DateTime Date
);

public record ReportRequestDto(
    DateTime? DateFrom,
    DateTime? DateTo,
    Guid? ServantId,
    string? Area,
    string? College
);

public record ServantActivitySummaryDto(
    Guid ServantId,
    string FullName,
    int AssignedCount,
    int CallsInPeriod,
    int VisitsInPeriod
);

public record StudentNoContactDto(
    Guid StudentId,
    string FullName,
    Guid? ServantId,
    string? ServantName,
    DateTime? LastContactDate
);

public record StudentsByGroupDto(
    string GroupKey,
    int Count,
    IReadOnlyList<Guid>? StudentIds
);

public record PagedReportResult<T>(IReadOnlyList<T> Items, int TotalCount, int Page, int PageSize);
