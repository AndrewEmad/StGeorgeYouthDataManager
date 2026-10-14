namespace YouthDataManager.Students.Service.Abstractions.DTOs;

public sealed record SelectedStudentVersion(Guid StudentId, string? Version);
public sealed record BulkAcademicYearRequest(List<SelectedStudentVersion>? Students, string? AcademicYear);
public sealed record BulkAcademicYearOutcome(Guid StudentId, bool Success, string? Error = null,
    string? Code = null, string? FullName = null, string? AcademicYear = null, string? Version = null, bool Changed = false);
public sealed record BulkAcademicYearResult(IReadOnlyList<BulkAcademicYearOutcome> Outcomes);
