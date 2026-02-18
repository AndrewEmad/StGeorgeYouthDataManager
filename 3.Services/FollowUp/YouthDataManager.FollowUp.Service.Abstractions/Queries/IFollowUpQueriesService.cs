using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using YouthDataManager.FollowUp.Service.Abstractions.DTOs;
using YouthDataManager.Shared.Service.Abstractions;

namespace YouthDataManager.FollowUp.Service.Abstractions.Queries;

public interface IFollowUpQueriesService
{
    Task<ServiceResult<IEnumerable<CallLogDto>>> GetStudentCallHistory(Guid studentId);
    Task<ServiceResult<IEnumerable<HomeVisitDto>>> GetStudentVisitHistory(Guid studentId);
    Task<ServiceResult<IEnumerable<CallLogDto>>> GetServantCallHistory(Guid servantId);
    Task<ServiceResult<IEnumerable<HomeVisitDto>>> GetServantVisitHistory(Guid servantId);
}
