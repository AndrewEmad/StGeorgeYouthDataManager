using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using YouthDataManager.Domain.Repositories.NoTracking;
using YouthDataManager.FollowUp.Service.Abstractions.DTOs;
using YouthDataManager.FollowUp.Service.Abstractions.Queries;
using YouthDataManager.Shared.Service.Abstractions;

namespace YouthDataManager.FollowUp.Service.Implementations.Queries;

public class FollowUpQueriesService : IFollowUpQueriesService
{
    private readonly ICallLogNoTrackingRepository _callRepository;
    private readonly IHomeVisitNoTrackingRepository _visitRepository;
    private readonly ILogger<FollowUpQueriesService> _logger;

    public FollowUpQueriesService(
        ICallLogNoTrackingRepository callRepository,
        IHomeVisitNoTrackingRepository visitRepository,
        ILogger<FollowUpQueriesService> logger)
    {
        _callRepository = callRepository;
        _visitRepository = visitRepository;
        _logger = logger;
    }

    public async Task<ServiceResult<IEnumerable<CallLogDto>>> GetStudentCallHistory(Guid studentId)
    {
        try
        {
            var result = await _callRepository.GetByStudentId(studentId, c => new CallLogDto(
                c.Id,
                c.StudentId,
                c.Student.FullName,
                c.CallDate,
                c.CallStatus,
                c.Notes,
                c.NextFollowUpDate
            ));

            return ServiceResult<IEnumerable<CallLogDto>>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting call history for student {StudentId}", studentId);
            return ServiceResult<IEnumerable<CallLogDto>>.Failure("An error occurred while retrieving call history.");
        }
    }

    public async Task<ServiceResult<IEnumerable<HomeVisitDto>>> GetStudentVisitHistory(Guid studentId)
    {
        try
        {
            var result = await _visitRepository.GetByStudentId(studentId, v => new HomeVisitDto(
                v.Id,
                v.StudentId,
                v.Student.FullName,
                v.VisitDate,
                v.VisitOutcome,
                v.Notes,
                v.NextVisitDate
            ));

            return ServiceResult<IEnumerable<HomeVisitDto>>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting visit history for student {StudentId}", studentId);
            return ServiceResult<IEnumerable<HomeVisitDto>>.Failure("An error occurred while retrieving visit history.");
        }
    }

    public async Task<ServiceResult<IEnumerable<CallLogDto>>> GetServantCallHistory(Guid servantId)
    {
        try
        {
            var result = await _callRepository.GetByServantId(servantId, c => new CallLogDto(
                c.Id,
                c.StudentId,
                c.Student.FullName,
                c.CallDate,
                c.CallStatus,
                c.Notes,
                c.NextFollowUpDate
            ));

            return ServiceResult<IEnumerable<CallLogDto>>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting call history for servant {ServantId}", servantId);
            return ServiceResult<IEnumerable<CallLogDto>>.Failure("An error occurred while retrieving call history.");
        }
    }

    public async Task<ServiceResult<IEnumerable<HomeVisitDto>>> GetServantVisitHistory(Guid servantId)
    {
        try
        {
            var result = await _visitRepository.GetByServantId(servantId, v => new HomeVisitDto(
                v.Id,
                v.StudentId,
                v.Student.FullName,
                v.VisitDate,
                v.VisitOutcome,
                v.Notes,
                v.NextVisitDate
            ));

            return ServiceResult<IEnumerable<HomeVisitDto>>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting visit history for servant {ServantId}", servantId);
            return ServiceResult<IEnumerable<HomeVisitDto>>.Failure("An error occurred while retrieving visit history.");
        }
    }
}
