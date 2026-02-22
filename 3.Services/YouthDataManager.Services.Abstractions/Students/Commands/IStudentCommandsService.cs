using System;
using System.Threading.Tasks;
using YouthDataManager.Shared.Service.Abstractions;
using YouthDataManager.Students.Service.Abstractions.DTOs;

namespace YouthDataManager.Students.Service.Abstractions.Commands;

public interface IStudentCommandsService
{
    Task<ServiceResult<Guid>> Create(CreateStudentRequest request);
    Task<ServiceResult> Update(UpdateStudentRequest request, Guid? currentUserId, bool isAdmin);
    Task<ServiceResult> Delete(Guid id);
    Task<ServiceResult> AssignToServant(Guid studentId, Guid servantId);
    Task<ServiceResult> UnassignFromServant(Guid studentId);
}
