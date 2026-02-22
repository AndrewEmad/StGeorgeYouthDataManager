using System;
using System.Collections.Generic;

namespace YouthDataManager.Students.Service.Abstractions.DTOs;

public record PagedResult<T>(IReadOnlyList<T> Items, int TotalCount, int Page, int PageSize);

public record StudentListFilter(
    string? Search,
    string? Area,
    string? AcademicYear,
    int? Gender,
    Guid? ServantId,
    bool? HasServant,
    string? SortBy,
    bool? SortDesc,
    IEnumerable<Guid>? ExcludeStudentIds = null
);

public record UnassignedStudentForServantDto(StudentDto Student, bool HasPendingRequestByMe);

/// <summary>Single paged list for servant: Assigned, then Requested, then Unassigned.</summary>
public record ServantStudentPageItemDto(StudentDto Student, string Segment);

public record StudentDto(
    Guid Id,
    string FullName,
    string Phone,
    string? SecondaryPhone,
    string? Address,
    string? Area,
    string? College,
    string? AcademicYear,
    string? ConfessionFather,
    string? Notes,
    string? ServantName,
    Guid? ServantId,
    DateTime? BirthDate,
    int Gender,
    DateTime? LastFollowUpDate,
    string? LastFollowUpNote,
    DateTime? LastAttendanceDate,
    DateTime? CreatedAt,
    DateTime? UpdatedAt
);

public record CreateStudentRequest(
    string FullName,
    string Phone,
    string? SecondaryPhone,
    string? Address,
    string? Area,
    string? College,
    string? AcademicYear,
    string? ConfessionFather,
    string? Notes,
    YouthDataManager.Domain.Enums.Gender Gender,
    Guid? ServantId,
    DateTime? BirthDate
);

public record UpdateStudentRequest(
    Guid Id,
    string FullName,
    string Phone,
    string? Address,
    string? Area,
    string? College,
    string? AcademicYear,
    string? ConfessionFather,
    YouthDataManager.Domain.Enums.Gender Gender,
    Guid? ServantId,
    DateTime? BirthDate
);

// Removal request (servant requests to remove student; admin approves → unassign and/or flag deleted per RemovalType)
public record StudentRemovalRequestDto(
    Guid Id,
    Guid StudentId,
    string StudentName,
    Guid RequestedByUserId,
    string RequestedByUserName,
    DateTime RequestedAt,
    string Status,
    int RemovalType,
    DateTime? ProcessedAt,
    Guid? ProcessedByUserId
);

public record CreateRemovalRequestDto(Guid StudentId, int RemovalType = 1);

// Assignment request (servant requests to be assigned; admin approves → assign)
public record StudentAssignmentRequestDto(
    Guid Id,
    Guid StudentId,
    string StudentName,
    Guid RequestedByUserId,
    string RequestedByUserName,
    DateTime RequestedAt,
    string Status,
    DateTime? ProcessedAt,
    Guid? ProcessedByUserId
);

public record CreateAssignmentRequestDto(Guid StudentId);

// Addition request (servant requests to add a new student; admin approves → create student and assign to servant)
public record StudentAdditionRequestDto(
    Guid Id,
    string FullName,
    string Phone,
    string? SecondaryPhone,
    string? Area,
    string? College,
    string? AcademicYear,
    string? Notes,
    int Gender,
    Guid RequestedByUserId,
    string RequestedByUserName,
    DateTime RequestedAt,
    string Status
);

public record CreateStudentAdditionRequestDto(
    string FullName,
    string Phone,
    string? SecondaryPhone,
    string? Address,
    string? Area,
    string? College,
    string? AcademicYear,
    string? ConfessionFather,
    string? Notes,
    int Gender,
    DateTime? BirthDate
);

public record StudentEditLogDto(
    Guid Id,
    Guid StudentId,
    DateTime UpdatedAt,
    Guid UpdatedByUserId,
    string UpdatedByUserName,
    string Details
);
