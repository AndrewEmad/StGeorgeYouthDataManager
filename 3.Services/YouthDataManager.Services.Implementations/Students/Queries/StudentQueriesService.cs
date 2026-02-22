using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using YouthDataManager.Domain.Entities;
using YouthDataManager.Domain.Repositories.NoTracking;
using YouthDataManager.Domain.Repositories.Tracking;
using YouthDataManager.Shared.Service.Abstractions;
using YouthDataManager.Students.Service.Abstractions.Queries;
using YouthDataManager.Students.Service.Abstractions.DTOs;

namespace YouthDataManager.Students.Service.Implementations.Queries;

public class StudentQueriesService : IStudentQueriesService
{
    private readonly IStudentNoTrackingRepository _repository;
    private readonly IStudentEditLogNoTrackingRepository _editLogRepository;
    private readonly IStudentAssignmentRequestRepository _assignmentRequestRepository;
    private readonly ILogger<StudentQueriesService> _logger;

    public StudentQueriesService(
        IStudentNoTrackingRepository repository,
        IStudentEditLogNoTrackingRepository editLogRepository,
        IStudentAssignmentRequestRepository assignmentRequestRepository,
        ILogger<StudentQueriesService> logger)
    {
        _repository = repository;
        _editLogRepository = editLogRepository;
        _assignmentRequestRepository = assignmentRequestRepository;
        _logger = logger;
    }

    public async Task<ServiceResult<StudentDto>> GetById(Guid id)
    {
        try
        {
            var result = await _repository.GetById(id, StudentDtoSelector());

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
                    : (s.HomeVisits.OrderByDescending(v => v.VisitDate).Select(v => v.Notes).FirstOrDefault() ?? s.CallLogs.OrderByDescending(c => c.CallDate).Select(c => c.Notes).FirstOrDefault()),
                s.LastAttendanceDate,
                s.CreatedAt,
                s.UpdatedAt
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
                    : (s.HomeVisits.OrderByDescending(v => v.VisitDate).Select(v => v.Notes).FirstOrDefault() ?? s.CallLogs.OrderByDescending(c => c.CallDate).Select(c => c.Notes).FirstOrDefault()),
                s.LastAttendanceDate,
                s.CreatedAt,
                s.UpdatedAt
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
                        : s.HomeVisits.OrderByDescending(v => v.VisitDate).Select(v => v.Notes).FirstOrDefault(),
                    s.LastAttendanceDate,
                    s.CreatedAt,
                    s.UpdatedAt),
                filter.ExcludeStudentIds);
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

    public async Task<ServiceResult<IEnumerable<StudentEditLogDto>>> GetEditHistory(Guid studentId)
    {
        try
        {
            var items = await _editLogRepository.GetByStudentId(studentId, e => new StudentEditLogDto(
                e.Id,
                e.StudentId,
                e.UpdatedAt,
                e.UpdatedByUserId,
                e.UpdatedByUserName,
                e.Details
            ));
            return ServiceResult<IEnumerable<StudentEditLogDto>>.Success(items);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting edit history for student {StudentId}", studentId);
            return ServiceResult<IEnumerable<StudentEditLogDto>>.Failure("An error occurred while retrieving edit history.");
        }
    }

    public async Task<ServiceResult<PagedResult<UnassignedStudentForServantDto>>> GetUnassignedForServant(Guid servantId, int page, int pageSize)
    {
        try
        {
            if (page < 1) page = 1;
            if (pageSize > 100) pageSize = 100;
            if (pageSize < 1) pageSize = 10;

            var excludeIds = await _assignmentRequestRepository.GetStudentIdsWithPendingRequestByOtherThan(servantId);
            var filter = new StudentListFilter(null, null, null, null, null, false, null, null, excludeIds);
            var pagedResult = await GetPaged(filter, page, pageSize);
            if (pagedResult.Status != ServiceResultStatus.Success || pagedResult.Data == null)
                return ServiceResult<PagedResult<UnassignedStudentForServantDto>>.Failure(pagedResult.Message ?? "Failed to get unassigned students.");

            var myPendingIds = (await _assignmentRequestRepository.GetStudentIdsWithPendingRequestByUser(servantId)).ToHashSet();
            var items = pagedResult.Data.Items.Select(s => new UnassignedStudentForServantDto(s, myPendingIds.Contains(s.Id))).ToList();
            var result = new PagedResult<UnassignedStudentForServantDto>(items, pagedResult.Data.TotalCount, page, pageSize);
            return ServiceResult<PagedResult<UnassignedStudentForServantDto>>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting unassigned students for servant {ServantId}", servantId);
            return ServiceResult<PagedResult<UnassignedStudentForServantDto>>.Failure("An error occurred while retrieving the list.");
        }
    }

    public async Task<ServiceResult<PagedResult<ServantStudentPageItemDto>>> GetPagedForServant(Guid servantId, int page, int pageSize, string? search, string? area = null, string? academicYear = null, int? gender = null)
    {
        try
        {
            if (page < 1) page = 1;
            if (pageSize > 50) pageSize = 50;
            if (pageSize < 1) pageSize = 10;

            var (pageData, totalCount) = await _repository.GetPagedForServantCombinedAsync(servantId, search, area, academicYear, gender, page, pageSize);
            if (pageData.Count == 0)
                return ServiceResult<PagedResult<ServantStudentPageItemDto>>.Success(new PagedResult<ServantStudentPageItemDto>(new List<ServantStudentPageItemDto>(), totalCount, page, pageSize));

            var ids = pageData.Select(x => x.Id).ToList();
            var students = await _repository.GetByIds(ids, StudentDtoSelector());
            var studentDict = students.ToDictionary(s => s.Id);
            var items = pageData.Select(p => new ServantStudentPageItemDto(
                studentDict[p.Id],
                p.Segment == 1 ? "Assigned" : p.Segment == 2 ? "Requested" : "Unassigned")).ToList();
            return ServiceResult<PagedResult<ServantStudentPageItemDto>>.Success(new PagedResult<ServantStudentPageItemDto>(items, totalCount, page, pageSize));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting paged students for servant {ServantId}", servantId);
            return ServiceResult<PagedResult<ServantStudentPageItemDto>>.Failure("An error occurred while retrieving the list.");
        }
    }

    private static Expression<Func<Student, StudentDto>> StudentDtoSelector()
    {
        return s => new StudentDto(
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
                : (s.HomeVisits.OrderByDescending(v => v.VisitDate).Select(v => v.Notes).FirstOrDefault() ?? s.CallLogs.OrderByDescending(c => c.CallDate).Select(c => c.Notes).FirstOrDefault()),
            s.LastAttendanceDate,
            s.CreatedAt,
            s.UpdatedAt
        );
    }
}
