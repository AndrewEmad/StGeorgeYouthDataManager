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
    IEnumerable<RecentActivityDto> RecentActivities,
    int StudentsNeedingFollowUp=0,
    int StudentsNotContacted=0,
    int UnassignedStudents=0,
    int UnassignedTasks=0
);

public record ServantPerformanceDto(
    Guid ServantId,
    string FullName,
    int AssignedStudentsCount,
    int CallsThisWeek,
    int VisitsThisWeek
)
{
    public ServantPerformanceDto() : this(Guid.Empty,"",0,0,0) { }
}

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
    string? College,
    string? Search = null,
    string? AcademicYear = null,
    int? Gender = null,
    int? BirthMonth = null,
    bool? HasServant = null
);

public record ServantActivitySummaryDto(
    Guid ServantId,
    string FullName,
    int AssignedCount,
    int CallsInPeriod,
    int VisitsInPeriod
)
{
    public ServantActivitySummaryDto() : this(Guid.Empty,"",0,0,0) { }
}

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

/// <summary>Servant (user) with stats for admin list; supports full-data sort then page.</summary>
public record ServantWithStatsDto(
    Guid Id,
    string UserName,
    string FullName,
    string Email,
    string? Phone,
    string Role,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    string? PhotoPath,
    string? Address,
    int AssignedStudentsCount,
    DateTime? LastCallDate,
    DateTime? LastVisitDate
)
{
    public ServantWithStatsDto() : this(Guid.Empty,"","","",null,"",false,default,null,null,null,0,null,null) { }
}
