namespace YouthDataManager.Students.Service.Abstractions.DTOs;
public sealed record BulkAreaRequest(List<SelectedStudentVersion>? Students,string? Area);
public sealed record BulkAreaOutcome(Guid StudentId,bool Success,string? Error=null,string? Code=null,string? FullName=null,string? Area=null,string? Version=null,bool Changed=false);
public sealed record BulkAreaResult(IReadOnlyList<BulkAreaOutcome> Outcomes);
