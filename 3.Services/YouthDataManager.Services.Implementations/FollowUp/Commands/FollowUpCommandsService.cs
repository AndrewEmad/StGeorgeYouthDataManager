using System;
using System.Collections.Generic;
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
            var visitId = Guid.NewGuid();
            var visit = new HomeVisit
            {
                Id = visitId,
                StudentId = request.StudentId,
                ServantId = currentUserId,
                VisitDate = request.VisitDate,
                VisitOutcome = request.VisitOutcome,
                Notes = request.Notes,
                NextVisitDate = request.NextVisitDate,
                CreatedAt = DateTime.UtcNow
            };

            var participantIds = new HashSet<Guid> { currentUserId };
            if (request.ParticipantServantIds != null)
            {
                foreach (var id in request.ParticipantServantIds)
                    participantIds.Add(id);
            }

            foreach (var servantId in participantIds)
            {
                visit.Participants.Add(new HomeVisitParticipant
                {
                    HomeVisitId = visitId,
                    ServantId = servantId
                });
            }

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

    public async Task<ServiceResult> UpdateVisit(Guid visitId, UpdateHomeVisitRequest request, Guid currentUserId)
    {
        try
        {
            var visit = await _visitRepository.GetByIdWithParticipants(visitId);
            if (visit == null)
                return ServiceResult.Failure("Visit not found.");

            if (visit.ServantId != currentUserId)
                return ServiceResult.Failure("Only the person who recorded the visit can edit it.");

            visit.VisitDate = request.VisitDate;
            visit.VisitOutcome = request.VisitOutcome;
            visit.Notes = request.Notes ?? string.Empty;
            visit.NextVisitDate = request.NextVisitDate;

            var participantIds = new HashSet<Guid> { currentUserId };
            if (request.ParticipantServantIds != null)
            {
                foreach (var id in request.ParticipantServantIds)
                    participantIds.Add(id);
            }

            visit.Participants.Clear();
            foreach (var servantId in participantIds)
            {
                visit.Participants.Add(new HomeVisitParticipant
                {
                    HomeVisitId = visit.Id,
                    ServantId = servantId
                });
            }

            _visitRepository.Update(visit);
            await _unitOfWork.SaveChangesAsync();

            return ServiceResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating visit {VisitId}", visitId);
            return ServiceResult.Failure("An error occurred while updating the visit.");
        }
    }
}
