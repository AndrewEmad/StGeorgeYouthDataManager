using Microsoft.EntityFrameworkCore;
using YouthDataManager.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using YouthDataManager.Domain.Enums;
using YouthDataManager.Domain.Repositories.Tracking;
using YouthDataManager.Students.Service.Abstractions.Commands;
using YouthDataManager.Students.Service.Abstractions.DTOs;
using YouthDataManager.Shared.Service.Abstractions;

namespace YouthDataManager.Students.Service.Implementations.Commands;

public class StudentRemovalRequestService : IStudentRemovalRequestService
{
    private readonly IStudentRemovalRequestRepository _requestRepository;
    private readonly IStudentRepository _studentRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IActivityLogger _activityLogger;
    private readonly IApplicationStore _store;
    private readonly ICallerContext _caller;
    private readonly ILogger<StudentRemovalRequestService> _logger;

    public StudentRemovalRequestService(
        IStudentRemovalRequestRepository requestRepository,
        IStudentRepository studentRepository,
        IUnitOfWork unitOfWork,
        IActivityLogger activityLogger, IApplicationStore store, ICallerContext caller,
        ILogger<StudentRemovalRequestService> logger)
    {
        _requestRepository = requestRepository;
        _studentRepository = studentRepository;
        _unitOfWork = unitOfWork;
        _activityLogger = activityLogger; _store = store; _caller=caller;
        _logger = logger;
    }

    public Task<ServiceResult> Approve(Guid requestId, Guid adminUserId) => _store.Transaction(() => ApproveCore(requestId, adminUserId), r => r.Status == ServiceResultStatus.Success);
    public Task<ServiceResult> Reject(Guid requestId, Guid adminUserId) => _store.Transaction(() => RejectCore(requestId, adminUserId), r => r.Status == ServiceResultStatus.Success);
    public Task<ServiceResult<Guid>> CreateRequest(Guid studentId, Guid requestedByUserId, RemovalRequestType removalType) => _store.Transaction(() => CreateRequestCore(studentId, requestedByUserId, removalType), r => r.Status == ServiceResultStatus.Success);

    private async Task<ServiceResult<Guid>> CreateRequestCore(Guid studentId, Guid requestedByUserId, RemovalRequestType removalType)
    {
        if (!Enum.IsDefined(removalType)) return ServiceResult<Guid>.Failure("Invalid removal type.");
        if (!await EligibleUsers.Query(_store).AnyAsync(u=>u.Id==requestedByUserId, _caller.CancellationToken)) return ServiceResult<Guid>.Failure("Requester is inactive or ineligible.");
        try
        {
            var student = await _studentRepository.GetById(studentId);
            if (student == null)
                return ServiceResult<Guid>.Failure("Student not found");
            if (student.ServantId != requestedByUserId)
                return ServiceResult<Guid>.Failure("You can only request removal for students assigned to you.");

            var existing = await _requestRepository.GetPendingByStudentAndUser(studentId, requestedByUserId);
            if (existing != null)
                return ServiceResult<Guid>.Failure("A pending removal request already exists for this student.");

            var request = new Domain.Entities.StudentRemovalRequest
            {
                Id = Guid.NewGuid(),
                StudentId = studentId,
                RequestedByUserId = requestedByUserId,
                RequestedAt = DateTime.UtcNow,
                Status = RemovalRequestStatus.Pending,
                RemovalType = removalType
            };
            _requestRepository.Add(request);
            await _unitOfWork.SaveChangesAsync();

            await _activityLogger.LogAsync("طلب إزالة مخدوم", $"طلب الخادم إزالة المخدوم: {student.FullName}");
            return ServiceResult<Guid>.Success(request.Id);
        }
        catch (DbUpdateException) { throw; }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating removal request for student {StudentId}", studentId);
            return ServiceResult<Guid>.Failure("An error occurred while creating the request.");
        }
    }

    public async Task<ServiceResult<IEnumerable<StudentRemovalRequestDto>>> GetPendingForAdmin()
    {
        try
        {
            var list = await _requestRepository.GetPendingRequests();
            var dtos = list.Select(r => MapToDto(r));
            return ServiceResult<IEnumerable<StudentRemovalRequestDto>>.Success(dtos);
        }
        catch (DbUpdateException) { throw; }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting pending removal requests");
            return ServiceResult<IEnumerable<StudentRemovalRequestDto>>.Failure("An error occurred.");
        }
    }

    public async Task<ServiceResult<IEnumerable<StudentRemovalRequestDto>>> GetMyRequests(Guid userId)
    {
        try
        {
            var list = await _requestRepository.GetByRequestedByUser(userId);
            var dtos = list.Select(r => MapToDto(r));
            return ServiceResult<IEnumerable<StudentRemovalRequestDto>>.Success(dtos);
        }
        catch (DbUpdateException) { throw; }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting removal requests for user {UserId}", userId);
            return ServiceResult<IEnumerable<StudentRemovalRequestDto>>.Failure("An error occurred.");
        }
    }

    public async Task<ServiceResult<StudentRemovalRequestDto?>> GetPendingForStudentAndUser(Guid studentId, Guid userId)
    {
        try
        {
            var r = await _requestRepository.GetPendingByStudentAndUser(studentId, userId);
            return ServiceResult<StudentRemovalRequestDto?>.Success(r != null ? MapToDto(r) : null);
        }
        catch (DbUpdateException) { throw; }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting pending request for student {StudentId}", studentId);
            return ServiceResult<StudentRemovalRequestDto?>.Failure("An error occurred.");
        }
    }

    private async Task<ServiceResult> ApproveCore(Guid requestId, Guid adminUserId)
    {
        try
        {
            var request = await _requestRepository.GetById(requestId);
            if (request == null)
                return ServiceResult.Failure("Request not found");
            if (request.Status == RemovalRequestStatus.Approved) return ServiceResult.Success();
            if (_caller.ExpectedVersion!=null && _caller.ExpectedVersion!=Convert.ToBase64String(request.Version)) return ServiceResult.Failure("Request changed. Refresh before retrying.",new ServiceError("Conflict"));
            if (request.Status != RemovalRequestStatus.Pending)
                return ServiceResult.Failure("Request is already processed.",new ServiceError("Conflict"));
            if (!await EligibleUsers.Query(_store).AnyAsync(u=>u.Id==request.RequestedByUserId, _caller.CancellationToken)) return ServiceResult.Failure("Requester is inactive or ineligible.",new ServiceError("Conflict"));


            var currentStudent=await _studentRepository.GetById(request.StudentId);
            if (currentStudent==null || currentStudent.ServantId!=request.RequestedByUserId) return ServiceResult.Failure("Ownership or student status changed.",new ServiceError("Conflict"));
            request.Status = RemovalRequestStatus.Approved;
            request.ProcessedAt = DateTime.UtcNow;
            request.ProcessedByUserId = adminUserId;
            request.ProcessingReason=_caller.Reason?.Trim();
            _requestRepository.Update(request);

            var student = await _studentRepository.GetById(request.StudentId);
            if (student != null)
            {
                student.ServantId = null;
                if (request.RemovalType == RemovalRequestType.DeleteFromSystem)
                {
                    student.IsDeleted = true;
                    await StudentCommandsService.ArchiveWork(_store,student.Id,_caller);
                }
                foreach (var task in await _store.Query<FollowUpTask>().Where(t=>t.StudentId==student.Id && (t.Status=="Open" || t.Status=="PendingReview")).ToListAsync(_caller.CancellationToken)) task.AssigneeId=null;
                student.UpdatedAt = DateTime.UtcNow;
                _studentRepository.Update(student);
            }

            await _unitOfWork.SaveChangesAsync();
            await _activityLogger.LogAsync("موافقة على طلب إزالة مخدوم", $"تمت الموافقة على طلب إزالة: {student?.FullName}");
            return ServiceResult.Success();
        }
        catch (DbUpdateException) { throw; }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving removal request {RequestId}", requestId);
            return ServiceResult.Failure("An error occurred while approving the request.");
        }
    }

    private async Task<ServiceResult> RejectCore(Guid requestId, Guid adminUserId)
    {
        try
        {
            var request = await _requestRepository.GetById(requestId);
            if (request == null)
                return ServiceResult.Failure("Request not found");
            if (_caller.ExpectedVersion!=null && _caller.ExpectedVersion!=Convert.ToBase64String(request.Version)) return ServiceResult.Failure("Request changed. Refresh before retrying.",new ServiceError("Conflict"));
            if (request.Status == RemovalRequestStatus.Rejected) return ServiceResult.Success();
            if (request.Status != RemovalRequestStatus.Pending)
                return ServiceResult.Failure("Request is already processed.",new ServiceError("Conflict"));

            request.Status = RemovalRequestStatus.Rejected;
            request.ProcessedAt = DateTime.UtcNow;
            request.ProcessedByUserId = adminUserId;
            request.ProcessingReason=_caller.Reason?.Trim();
            _requestRepository.Update(request);
            await _unitOfWork.SaveChangesAsync();
            return ServiceResult.Success();
        }
        catch (DbUpdateException) { throw; }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rejecting removal request {RequestId}", requestId);
            return ServiceResult.Failure("An error occurred while rejecting the request.");
        }
    }

    private static StudentRemovalRequestDto MapToDto(Domain.Entities.StudentRemovalRequest r)
    {
        return new StudentRemovalRequestDto(
            r.Id,
            r.StudentId,
            r.Student?.FullName ?? "",
            r.RequestedByUserId,
            r.RequestedByUser?.FullName ?? "",
            r.RequestedAt,
            r.Status.ToString(),
            (int)r.RemovalType,
            r.ProcessedAt,
            r.ProcessedByUserId
        );
    }
}
