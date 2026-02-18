using System;
using System.Threading.Tasks;
using YouthDataManager.FollowUp.Service.Abstractions.DTOs;
using YouthDataManager.Shared.Service.Abstractions;

namespace YouthDataManager.FollowUp.Service.Abstractions.Commands;

public interface IFollowUpCommandsService
{
    Task<ServiceResult<Guid>> RegisterCall(CreateCallLogRequest request, Guid currentUserId);
    Task<ServiceResult<Guid>> RegisterVisit(CreateHomeVisitRequest request, Guid currentUserId);
}
