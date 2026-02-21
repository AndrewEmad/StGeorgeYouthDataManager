using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using YouthDataManager.Domain.Enums;
using YouthDataManager.Students.Service.Abstractions.DTOs;
using YouthDataManager.Shared.Service.Abstractions;

namespace YouthDataManager.Students.Service.Abstractions.Commands;

public interface IStudentRemovalRequestService
{
    Task<ServiceResult<Guid>> CreateRequest(Guid studentId, Guid requestedByUserId, RemovalRequestType removalType);
    Task<ServiceResult<IEnumerable<StudentRemovalRequestDto>>> GetPendingForAdmin();
    Task<ServiceResult<IEnumerable<StudentRemovalRequestDto>>> GetMyRequests(Guid userId);
    Task<ServiceResult<StudentRemovalRequestDto?>> GetPendingForStudentAndUser(Guid studentId, Guid userId);
    Task<ServiceResult> Approve(Guid requestId, Guid adminUserId);
    Task<ServiceResult> Reject(Guid requestId, Guid adminUserId);
}
