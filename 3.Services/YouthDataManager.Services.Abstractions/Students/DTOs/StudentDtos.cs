using YouthDataManager.Shared.Service.Abstractions;
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
    int? BirthMonth = null,
    IEnumerable<Guid>? ExcludeStudentIds = null
);

public record StudentDiscoveryDto(Guid Id,string FullName,string Area,string College,string AcademicYear);

public record UnassignedStudentForServantDto(object Student, bool HasPendingRequestByMe);

/// <summary>Single paged list for servant: Assigned, then Requested, then Unassigned.</summary>
public record ServantStudentPageItemDto(object Student, string Segment);

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
    DateTime? UpdatedAt,
    string? PhotoPath = null,
    int? BirthMonth = null,
    byte[]? Version = null,
    bool IsArchived = false
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
    DateTime? BirthDate,
    int? BirthMonth = null
);

public record UpdateStudentRequest(
    Guid Id,
    OptionalField<string?> FullName = default,
    OptionalField<string?> Phone = default,
    OptionalField<string?> Address = default,
    OptionalField<string?> Area = default,
    OptionalField<string?> College = default,
    OptionalField<string?> AcademicYear = default,
    OptionalField<string?> ConfessionFather = default,
    OptionalField<YouthDataManager.Domain.Enums.Gender?> Gender = default,
    OptionalField<Guid?> ServantId = default,
    OptionalField<DateTime?> BirthDate = default,
    OptionalField<int?> BirthMonth = default,
    OptionalField<string?> SecondaryPhone = default,
    OptionalField<string?> Notes = default
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
    string Status,
    string? Address = null,
    string? ConfessionFather = null,
    DateTime? BirthDate = null,
    DateTime? ProcessedAt = null,
    Guid? ProcessedByUserId = null,
    Guid? CreatedStudentId = null,
    int? BirthMonth = null,
    string? ProcessingReason = null
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
    DateTime? BirthDate,
    int? BirthMonth = null
);

public record StudentEditLogDto(
    Guid Id,
    Guid StudentId,
    DateTime UpdatedAt,
    Guid UpdatedByUserId,
    string UpdatedByUserName,
    string Details
);
