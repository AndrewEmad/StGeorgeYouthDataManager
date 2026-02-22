using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using YouthDataManager.Students.Service.Abstractions.DTOs;
using YouthDataManager.Shared.Service.Abstractions;

namespace YouthDataManager.Students.Service.Abstractions.Commands;

public interface IStudentAssignmentRequestService
{
    Task<ServiceResult<Guid>> CreateRequest(Guid studentId, Guid requestedByUserId);
    Task<ServiceResult<IEnumerable<StudentAssignmentRequestDto>>> GetPendingForAdmin();
    Task<ServiceResult<IEnumerable<StudentAssignmentRequestDto>>> GetMyRequests(Guid userId);
    Task<ServiceResult<StudentAssignmentRequestDto?>> GetPendingForStudent(Guid studentId);
    Task<ServiceResult> Approve(Guid requestId, Guid adminUserId);
    Task<ServiceResult> Reject(Guid requestId, Guid adminUserId);
}
