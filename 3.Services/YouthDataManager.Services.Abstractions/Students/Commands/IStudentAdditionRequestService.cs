using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using YouthDataManager.Students.Service.Abstractions.DTOs;
using YouthDataManager.Shared.Service.Abstractions;

namespace YouthDataManager.Students.Service.Abstractions.Commands;

public interface IStudentAdditionRequestService
{
    Task<ServiceResult<Guid>> Create(CreateStudentAdditionRequestDto dto, Guid requestedByUserId);
    Task<ServiceResult<IEnumerable<StudentAdditionRequestDto>>> GetPendingForAdmin();
    Task<ServiceResult<IEnumerable<StudentAdditionRequestDto>>> GetMyRequests(Guid userId);
    Task<ServiceResult> Approve(Guid requestId, Guid adminUserId);
    Task<ServiceResult> Reject(Guid requestId, Guid adminUserId);
}
