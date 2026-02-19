using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using YouthDataManager.Shared.Service.Abstractions;
using YouthDataManager.Students.Service.Abstractions.DTOs;

namespace YouthDataManager.Students.Service.Abstractions.Queries;

public interface IStudentQueriesService
{
    Task<ServiceResult<StudentDto>> GetById(Guid id);
    Task<ServiceResult<IEnumerable<StudentDto>>> GetAll();
    Task<ServiceResult<IEnumerable<StudentDto>>> GetByServantId(Guid servantId);
    Task<ServiceResult<PagedResult<StudentDto>>> GetPaged(StudentListFilter filter, int page, int pageSize);
    Task<ServiceResult<IEnumerable<string>>> GetDistinctAreas();
    Task<ServiceResult<IEnumerable<string>>> GetDistinctAcademicYears();
    Task<ServiceResult<IEnumerable<StudentEditLogDto>>> GetEditHistory(Guid studentId);
}
