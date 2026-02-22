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

public class StudentAssignmentRequestService : IStudentAssignmentRequestService
{
    private readonly IStudentAssignmentRequestRepository _requestRepository;
    private readonly IStudentRepository _studentRepository;
    private readonly IStudentCommandsService _studentCommandsService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IActivityLogger _activityLogger;
    private readonly ILogger<StudentAssignmentRequestService> _logger;

    public StudentAssignmentRequestService(
        IStudentAssignmentRequestRepository requestRepository,
        IStudentRepository studentRepository,
        IStudentCommandsService studentCommandsService,
        IUnitOfWork unitOfWork,
        IActivityLogger activityLogger,
        ILogger<StudentAssignmentRequestService> logger)
    {
        _requestRepository = requestRepository;
        _studentRepository = studentRepository;
        _studentCommandsService = studentCommandsService;
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
            if (student.ServantId != null)
                return ServiceResult<Guid>.Failure("Student is already assigned to a servant.");

            var existingPending = await _requestRepository.GetPendingByStudent(studentId);
            if (existingPending != null)
                return ServiceResult<Guid>.Failure("A pending assignment request already exists for this student.");

            var request = new Domain.Entities.StudentAssignmentRequest
            {
                Id = Guid.NewGuid(),
                StudentId = studentId,
                RequestedByUserId = requestedByUserId,
                RequestedAt = DateTime.UtcNow,
                Status = AssignmentRequestStatus.Pending
            };
            _requestRepository.Add(request);
            await _unitOfWork.SaveChangesAsync();

            await _activityLogger.LogAsync("طلب تخصيص مخدوم", $"طلب الخادم تخصيص المخدوم: {student.FullName}");
            return ServiceResult<Guid>.Success(request.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating assignment request for student {StudentId}", studentId);
            return ServiceResult<Guid>.Failure("An error occurred while creating the request.");
        }
    }

    public async Task<ServiceResult<IEnumerable<StudentAssignmentRequestDto>>> GetPendingForAdmin()
    {
        try
        {
            var list = await _requestRepository.GetPendingRequests();
            var dtos = list.Select(r => MapToDto(r));
            return ServiceResult<IEnumerable<StudentAssignmentRequestDto>>.Success(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting pending assignment requests");
            return ServiceResult<IEnumerable<StudentAssignmentRequestDto>>.Failure("An error occurred.");
        }
    }

    public async Task<ServiceResult<IEnumerable<StudentAssignmentRequestDto>>> GetMyRequests(Guid userId)
    {
        try
        {
            var list = await _requestRepository.GetByRequestedByUser(userId);
            var dtos = list.Select(r => MapToDto(r));
            return ServiceResult<IEnumerable<StudentAssignmentRequestDto>>.Success(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting assignment requests for user {UserId}", userId);
            return ServiceResult<IEnumerable<StudentAssignmentRequestDto>>.Failure("An error occurred.");
        }
    }

    public async Task<ServiceResult<StudentAssignmentRequestDto?>> GetPendingForStudent(Guid studentId)
    {
        try
        {
            var r = await _requestRepository.GetPendingByStudent(studentId);
            return ServiceResult<StudentAssignmentRequestDto?>.Success(r != null ? MapToDto(r) : null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting pending assignment request for student {StudentId}", studentId);
            return ServiceResult<StudentAssignmentRequestDto?>.Failure("An error occurred.");
        }
    }

    public async Task<ServiceResult> Approve(Guid requestId, Guid adminUserId)
    {
        try
        {
            var request = await _requestRepository.GetById(requestId);
            if (request == null)
                return ServiceResult.Failure("Request not found");
            if (request.Status != AssignmentRequestStatus.Pending)
                return ServiceResult.Failure("Request is already processed.");

            var assignResult = await _studentCommandsService.AssignToServant(request.StudentId, request.RequestedByUserId);
            if (assignResult.Status != ServiceResultStatus.Success)
                return assignResult;

            request.Status = AssignmentRequestStatus.Approved;
            request.ProcessedAt = DateTime.UtcNow;
            request.ProcessedByUserId = adminUserId;
            _requestRepository.Update(request);

            await _unitOfWork.SaveChangesAsync();
            await _activityLogger.LogAsync("موافقة على طلب تخصيص مخدوم", $"تمت الموافقة على طلب تخصيص: {request.Student?.FullName}");
            return ServiceResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving assignment request {RequestId}", requestId);
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
            if (request.Status != AssignmentRequestStatus.Pending)
                return ServiceResult.Failure("Request is already processed.");

            request.Status = AssignmentRequestStatus.Rejected;
            request.ProcessedAt = DateTime.UtcNow;
            request.ProcessedByUserId = adminUserId;
            _requestRepository.Update(request);
            await _unitOfWork.SaveChangesAsync();
            return ServiceResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rejecting assignment request {RequestId}", requestId);
            return ServiceResult.Failure("An error occurred while rejecting the request.");
        }
    }

    private static StudentAssignmentRequestDto MapToDto(Domain.Entities.StudentAssignmentRequest r)
    {
        return new StudentAssignmentRequestDto(
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
