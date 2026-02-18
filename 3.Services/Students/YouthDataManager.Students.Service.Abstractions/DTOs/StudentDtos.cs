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
    bool? SortDesc
);

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
    string? LastFollowUpNote
);

public record CreateStudentRequest(
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

// Removal request (servant requests to remove student; admin approves → unassign + flag student as deleted)
public record StudentRemovalRequestDto(
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

public record CreateRemovalRequestDto(Guid StudentId);
