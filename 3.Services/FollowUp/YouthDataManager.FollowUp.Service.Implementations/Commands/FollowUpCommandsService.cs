using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using YouthDataManager.Domain.Entities;
using YouthDataManager.Domain.Repositories.Tracking;
using YouthDataManager.FollowUp.Service.Abstractions.Commands;
using YouthDataManager.FollowUp.Service.Abstractions.DTOs;
using YouthDataManager.Shared.Service.Abstractions;

namespace YouthDataManager.FollowUp.Service.Implementations.Commands;

public class FollowUpCommandsService : IFollowUpCommandsService
{
    private readonly ICallLogRepository _callRepository;
    private readonly IHomeVisitRepository _visitRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<FollowUpCommandsService> _logger;

    public FollowUpCommandsService(
        ICallLogRepository callRepository,
        IHomeVisitRepository visitRepository,
        IUnitOfWork unitOfWork,
        ILogger<FollowUpCommandsService> logger)
    {
        _callRepository = callRepository;
        _visitRepository = visitRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<ServiceResult<Guid>> RegisterCall(CreateCallLogRequest request, Guid currentUserId)
    {
        try
        {
            var callLog = new CallLog
            {
                Id = Guid.NewGuid(),
                StudentId = request.StudentId,
                ServantId = currentUserId,
                CallDate = request.CallDate,
                CallStatus = request.CallStatus,
                Notes = request.Notes,
                NextFollowUpDate = request.NextFollowUpDate,
                CreatedAt = DateTime.UtcNow
            };

            _callRepository.Add(callLog);
            await _unitOfWork.SaveChangesAsync();

            return ServiceResult<Guid>.Success(callLog.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering call for student {StudentId}", request.StudentId);
            return ServiceResult<Guid>.Failure("An error occurred while registering the call.");
        }
    }

    public async Task<ServiceResult<Guid>> RegisterVisit(CreateHomeVisitRequest request, Guid currentUserId)
    {
        try
        {
            var visit = new HomeVisit
            {
                Id = Guid.NewGuid(),
                StudentId = request.StudentId,
                ServantId = currentUserId,
                VisitDate = request.VisitDate,
                VisitOutcome = request.VisitOutcome,
                Notes = request.Notes,
                NextVisitDate = request.NextVisitDate,
                CreatedAt = DateTime.UtcNow
            };

            _visitRepository.Add(visit);
            await _unitOfWork.SaveChangesAsync();

            return ServiceResult<Guid>.Success(visit.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering visit for student {StudentId}", request.StudentId);
            return ServiceResult<Guid>.Failure("An error occurred while registering the visit.");
        }
    }
}
