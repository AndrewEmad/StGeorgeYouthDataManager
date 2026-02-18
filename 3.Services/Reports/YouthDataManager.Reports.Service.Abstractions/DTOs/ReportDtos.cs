using System;
using System.Collections.Generic;

namespace YouthDataManager.Reports.Service.Abstractions.DTOs;

public record ServantDashboardDto(
    int TotalStudents,
    int CallsToday,
    int VisitsToday,
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
    IEnumerable<ServantPerformanceDto> ServantPerformances,
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
