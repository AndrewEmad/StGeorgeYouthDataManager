using System;

namespace YouthDataManager.FollowUp.Service.Abstractions.DTOs;

public record CallLogDto(
    Guid Id,
    Guid StudentId,
    string StudentName,
    DateTime CallDate,
    YouthDataManager.Domain.Enums.CallStatus CallStatus,
    string Notes,
    DateTime? NextFollowUpDate
);

public record CreateCallLogRequest(
    Guid StudentId,
    DateTime CallDate,
    YouthDataManager.Domain.Enums.CallStatus CallStatus,
    string Notes,
    DateTime? NextFollowUpDate
);

public record HomeVisitDto(
    Guid Id,
    Guid StudentId,
    string StudentName,
    DateTime VisitDate,
    YouthDataManager.Domain.Enums.VisitOutcome VisitOutcome,
    string Notes,
    DateTime? NextVisitDate
);

public record CreateHomeVisitRequest(
    Guid StudentId,
    DateTime VisitDate,
    YouthDataManager.Domain.Enums.VisitOutcome VisitOutcome,
    string Notes,
    DateTime? NextVisitDate
);
