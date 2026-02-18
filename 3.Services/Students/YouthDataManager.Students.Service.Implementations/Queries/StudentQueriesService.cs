using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using YouthDataManager.Domain.Repositories.NoTracking;
using YouthDataManager.Shared.Service.Abstractions;
using YouthDataManager.Students.Service.Abstractions.Queries;
using YouthDataManager.Students.Service.Abstractions.DTOs;

namespace YouthDataManager.Students.Service.Implementations.Queries;

public class StudentQueriesService : IStudentQueriesService
{
    private readonly IStudentNoTrackingRepository _repository;
    private readonly ILogger<StudentQueriesService> _logger;

    public StudentQueriesService(
        IStudentNoTrackingRepository repository,
        ILogger<StudentQueriesService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<ServiceResult<StudentDto>> GetById(Guid id)
    {
        try
        {
            var result = await _repository.GetById(id, s => new StudentDto(
                s.Id,
                s.FullName,
                s.Phone,
                s.SecondaryPhone,
                s.Address,
                s.Area,
                s.College,
                s.AcademicYear,
                s.ConfessionFather,
                s.Notes,
                s.Servant != null ? s.Servant.FullName : null,
                s.ServantId,
                s.BirthDate,
                (int)s.Gender,
                s.CallLogs.Select(c => (DateTime?)c.CallDate).Concat(s.HomeVisits.Select(v => (DateTime?)v.VisitDate)).Max(),
                (s.CallLogs.Any() && (!s.HomeVisits.Any() || s.CallLogs.Max(c => c.CallDate) >= s.HomeVisits.Max(v => v.VisitDate)))
                    ? (s.CallLogs.OrderByDescending(c => c.CallDate).Select(c => c.Notes).FirstOrDefault() ?? s.HomeVisits.OrderByDescending(v => v.VisitDate).Select(v => v.Notes).FirstOrDefault())
                    : (s.HomeVisits.OrderByDescending(v => v.VisitDate).Select(v => v.Notes).FirstOrDefault() ?? s.CallLogs.OrderByDescending(c => c.CallDate).Select(c => c.Notes).FirstOrDefault())
            ));

            if (result == null)
                return ServiceResult<StudentDto>.Failure("Student not found");

            return ServiceResult<StudentDto>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting student {StudentId}", id);
            return ServiceResult<StudentDto>.Failure("An error occurred while retrieving student data.");
        }
    }

    public async Task<ServiceResult<IEnumerable<StudentDto>>> GetAll()
    {
        try
        {
            var result = await _repository.GetAll(s => new StudentDto(
                s.Id,
                s.FullName,
                s.Phone,
                s.SecondaryPhone,
                s.Address,
                s.Area,
                s.College,
                s.AcademicYear,
                s.ConfessionFather,
                s.Notes,
                s.Servant != null ? s.Servant.FullName : null,
                s.ServantId,
                s.BirthDate,
                (int)s.Gender,
                s.CallLogs.Select(c => (DateTime?)c.CallDate).Concat(s.HomeVisits.Select(v => (DateTime?)v.VisitDate)).Max(),
                (s.CallLogs.Any() && (!s.HomeVisits.Any() || s.CallLogs.Max(c => c.CallDate) >= s.HomeVisits.Max(v => v.VisitDate)))
                    ? (s.CallLogs.OrderByDescending(c => c.CallDate).Select(c => c.Notes).FirstOrDefault() ?? s.HomeVisits.OrderByDescending(v => v.VisitDate).Select(v => v.Notes).FirstOrDefault())
                    : (s.HomeVisits.OrderByDescending(v => v.VisitDate).Select(v => v.Notes).FirstOrDefault() ?? s.CallLogs.OrderByDescending(c => c.CallDate).Select(c => c.Notes).FirstOrDefault())
            ));

            return ServiceResult<IEnumerable<StudentDto>>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all students");
            return ServiceResult<IEnumerable<StudentDto>>.Failure("An error occurred while retrieving students list.");
        }
    }

    public async Task<ServiceResult<IEnumerable<StudentDto>>> GetByServantId(Guid servantId)
    {
        try
        {
            var result = await _repository.GetByServantId(servantId, s => new StudentDto(
                s.Id,
                s.FullName,
                s.Phone,
                s.SecondaryPhone,
                s.Address,
                s.Area,
                s.College,
                s.AcademicYear,
                s.ConfessionFather,
                s.Notes,
                s.Servant != null ? s.Servant.FullName : null,
                s.ServantId,
                s.BirthDate,
                (int)s.Gender,
                s.CallLogs.Select(c => (DateTime?)c.CallDate).Concat(s.HomeVisits.Select(v => (DateTime?)v.VisitDate)).Max(),
                (s.CallLogs.Any() && (!s.HomeVisits.Any() || s.CallLogs.Max(c => c.CallDate) >= s.HomeVisits.Max(v => v.VisitDate)))
                    ? (s.CallLogs.OrderByDescending(c => c.CallDate).Select(c => c.Notes).FirstOrDefault() ?? s.HomeVisits.OrderByDescending(v => v.VisitDate).Select(v => v.Notes).FirstOrDefault())
                    : (s.HomeVisits.OrderByDescending(v => v.VisitDate).Select(v => v.Notes).FirstOrDefault() ?? s.CallLogs.OrderByDescending(c => c.CallDate).Select(c => c.Notes).FirstOrDefault())
            ));

            return ServiceResult<IEnumerable<StudentDto>>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting students for servant {ServantId}", servantId);
            return ServiceResult<IEnumerable<StudentDto>>.Failure("An error occurred while retrieving assigned students list.");
        }
    }

    public async Task<ServiceResult<PagedResult<StudentDto>>> GetPaged(StudentListFilter filter, int page, int pageSize)
    {
        try
        {
            if (page < 1) page = 1;
            if (pageSize > 50) pageSize = 50;
            if (pageSize < 1) pageSize = 10;
            var (items, totalCount) = await _repository.GetPaged(
                filter.Search, filter.Area, filter.AcademicYear, filter.Gender, filter.ServantId, filter.HasServant,
                filter.SortBy, filter.SortDesc ?? false,
                page, pageSize,
                s => new StudentDto(
                    s.Id, s.FullName, s.Phone, s.SecondaryPhone, s.Address, s.Area, s.College, s.AcademicYear, s.ConfessionFather, s.Notes,
                    s.Servant != null ? s.Servant.FullName : null, s.ServantId, s.BirthDate, (int)s.Gender,
                    s.CallLogs.Select(c => (DateTime?)c.CallDate).Concat(s.HomeVisits.Select(v => (DateTime?)v.VisitDate)).Max(),
                    (s.CallLogs.Any() && (!s.HomeVisits.Any() || s.CallLogs.Max(c => c.CallDate) >= s.HomeVisits.Max(v => v.VisitDate)))
                        ? s.CallLogs.OrderByDescending(c => c.CallDate).Select(c => c.Notes).FirstOrDefault()
                        : s.HomeVisits.OrderByDescending(v => v.VisitDate).Select(v => v.Notes).FirstOrDefault()));
            var paged = new PagedResult<StudentDto>(items.ToList(), totalCount, page, pageSize);
            return ServiceResult<PagedResult<StudentDto>>.Success(paged);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting paged students");
            return ServiceResult<PagedResult<StudentDto>>.Failure("An error occurred while retrieving students.");
        }
    }

    public async Task<ServiceResult<IEnumerable<string>>> GetDistinctAreas()
    {
        try
        {
            var result = await _repository.GetDistinctAreasAsync();
            return ServiceResult<IEnumerable<string>>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting distinct areas");
            return ServiceResult<IEnumerable<string>>.Failure("An error occurred while retrieving areas.");
        }
    }

    public async Task<ServiceResult<IEnumerable<string>>> GetDistinctAcademicYears()
    {
        try
        {
            var result = await _repository.GetDistinctAcademicYearsAsync();
            return ServiceResult<IEnumerable<string>>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting distinct academic years");
            return ServiceResult<IEnumerable<string>>.Failure("An error occurred while retrieving academic years.");
        }
    }
}
