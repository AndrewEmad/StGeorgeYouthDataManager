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
    private readonly ILogger<StudentRemovalRequestService> _logger;

    public StudentRemovalRequestService(
        IStudentRemovalRequestRepository requestRepository,
        IStudentRepository studentRepository,
        IUnitOfWork unitOfWork,
        IActivityLogger activityLogger,
        ILogger<StudentRemovalRequestService> logger)
    {
        _requestRepository = requestRepository;
        _studentRepository = studentRepository;
        _unitOfWork = unitOfWork;
        _activityLogger = activityLogger;
        _logger = logger;
    }

    public async Task<ServiceResult<Guid>> CreateRequest(Guid studentId, Guid requestedByUserId)
    {
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
                Status = RemovalRequestStatus.Pending
            };
            _requestRepository.Add(request);
            await _unitOfWork.SaveChangesAsync();

            await _activityLogger.LogAsync("طلب إزالة مخدوم", $"طلب الخادم إزالة المخدوم: {student.FullName}");
            return ServiceResult<Guid>.Success(request.Id);
        }
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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting pending request for student {StudentId}", studentId);
            return ServiceResult<StudentRemovalRequestDto?>.Failure("An error occurred.");
        }
    }

    public async Task<ServiceResult> Approve(Guid requestId, Guid adminUserId)
    {
        try
        {
            var request = await _requestRepository.GetById(requestId);
            if (request == null)
                return ServiceResult.Failure("Request not found");
            if (request.Status != RemovalRequestStatus.Pending)
                return ServiceResult.Failure("Request is already processed.");

            request.Status = RemovalRequestStatus.Approved;
            request.ProcessedAt = DateTime.UtcNow;
            request.ProcessedByUserId = adminUserId;
            _requestRepository.Update(request);

            var student = await _studentRepository.GetById(request.StudentId);
            if (student != null)
            {
                student.ServantId = null;
                student.IsDeleted = true;
                student.UpdatedAt = DateTime.UtcNow;
                _studentRepository.Update(student);
            }

            await _unitOfWork.SaveChangesAsync();
            await _activityLogger.LogAsync("موافقة على طلب إزالة مخدوم", $"تمت الموافقة على طلب إزالة: {student?.FullName}");
            return ServiceResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving removal request {RequestId}", requestId);
            return ServiceResult.Failure("An error occurred while approving the request.");
        }
    }

    public async Task<ServiceResult> Reject(Guid requestId, Guid adminUserId)
    {
        try
        {
            var request = await _requestRepository.GetById(requestId);
            if (request == null)
                return ServiceResult.Failure("Request not found");
            if (request.Status != RemovalRequestStatus.Pending)
                return ServiceResult.Failure("Request is already processed.");

            request.Status = RemovalRequestStatus.Rejected;
            request.ProcessedAt = DateTime.UtcNow;
            request.ProcessedByUserId = adminUserId;
            _requestRepository.Update(request);
            await _unitOfWork.SaveChangesAsync();
            return ServiceResult.Success();
        }
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
            r.ProcessedAt,
            r.ProcessedByUserId
        );
    }
}
