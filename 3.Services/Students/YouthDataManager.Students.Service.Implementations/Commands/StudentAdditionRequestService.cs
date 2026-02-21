using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using YouthDataManager.Domain.Entities;
using YouthDataManager.Domain.Enums;
using YouthDataManager.Domain.Repositories.Tracking;
using YouthDataManager.Students.Service.Abstractions.Commands;
using YouthDataManager.Students.Service.Abstractions.DTOs;
using YouthDataManager.Shared.Service.Abstractions;

namespace YouthDataManager.Students.Service.Implementations.Commands;

public class StudentAdditionRequestService : IStudentAdditionRequestService
{
    private readonly IStudentAdditionRequestRepository _repo;
    private readonly IStudentCommandsService _studentCommandsService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IActivityLogger _activityLogger;
    private readonly ILogger<StudentAdditionRequestService> _logger;

    public StudentAdditionRequestService(
        IStudentAdditionRequestRepository repo,
        IStudentCommandsService studentCommandsService,
        IUnitOfWork unitOfWork,
        IActivityLogger activityLogger,
        ILogger<StudentAdditionRequestService> logger)
    {
        _repo = repo;
        _studentCommandsService = studentCommandsService;
        _unitOfWork = unitOfWork;
        _activityLogger = activityLogger;
        _logger = logger;
    }

    public async Task<ServiceResult<Guid>> Create(CreateStudentAdditionRequestDto dto, Guid requestedByUserId)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(dto.FullName) || string.IsNullOrWhiteSpace(dto.Phone))
                return ServiceResult<Guid>.Failure("الاسم ورقم الهاتف مطلوبان.");

            var request = new StudentAdditionRequest
            {
                Id = Guid.NewGuid(),
                RequestedByUserId = requestedByUserId,
                RequestedAt = DateTime.UtcNow,
                Status = AssignmentRequestStatus.Pending,
                FullName = dto.FullName.Trim(),
                Phone = dto.Phone.Trim(),
                SecondaryPhone = string.IsNullOrWhiteSpace(dto.SecondaryPhone) ? null : dto.SecondaryPhone.Trim(),
                Address = string.IsNullOrWhiteSpace(dto.Address) ? null : dto.Address.Trim(),
                Area = string.IsNullOrWhiteSpace(dto.Area) ? null : dto.Area.Trim(),
                College = string.IsNullOrWhiteSpace(dto.College) ? null : dto.College.Trim(),
                AcademicYear = string.IsNullOrWhiteSpace(dto.AcademicYear) ? null : dto.AcademicYear.Trim(),
                ConfessionFather = string.IsNullOrWhiteSpace(dto.ConfessionFather) ? null : dto.ConfessionFather.Trim(),
                Notes = string.IsNullOrWhiteSpace(dto.Notes) ? null : dto.Notes.Trim(),
                Gender = (Gender)dto.Gender,
                BirthDate = dto.BirthDate
            };

            _repo.Add(request);
            await _unitOfWork.SaveChangesAsync();
            await _activityLogger.LogAsync("طلب إضافة مخدوم", $"طلب الخادم إضافة مخدوم جديد: {request.FullName}");
            return ServiceResult<Guid>.Success(request.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating student addition request");
            return ServiceResult<Guid>.Failure("حدث خطأ أثناء إرسال الطلب.");
        }
    }

    public async Task<ServiceResult<IEnumerable<StudentAdditionRequestDto>>> GetPendingForAdmin()
    {
        try
        {
            var list = await _repo.GetPending();
            var dtos = list.Select(MapToDto).ToList();
            return ServiceResult<IEnumerable<StudentAdditionRequestDto>>.Success(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting pending addition requests");
            return ServiceResult<IEnumerable<StudentAdditionRequestDto>>.Failure("حدث خطأ أثناء تحميل الطلبات.");
        }
    }

    public async Task<ServiceResult<IEnumerable<StudentAdditionRequestDto>>> GetMyRequests(Guid userId)
    {
        try
        {
            var list = await _repo.GetByRequestedByUser(userId);
            var dtos = list.Select(MapToDto).ToList();
            return ServiceResult<IEnumerable<StudentAdditionRequestDto>>.Success(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting addition requests for user {UserId}", userId);
            return ServiceResult<IEnumerable<StudentAdditionRequestDto>>.Failure("حدث خطأ أثناء تحميل الطلبات.");
        }
    }

    public async Task<ServiceResult> Approve(Guid requestId, Guid adminUserId)
    {
        try
        {
            var request = await _repo.GetById(requestId);
            if (request == null)
                return ServiceResult.Failure("الطلب غير موجود.");
            if (request.Status != AssignmentRequestStatus.Pending)
                return ServiceResult.Failure("تمت معالجة هذا الطلب مسبقاً.");

            var createReq = new CreateStudentRequest(
                request.FullName,
                request.Phone,
                request.SecondaryPhone,
                request.Address ?? string.Empty,
                request.Area ?? string.Empty,
                request.College ?? string.Empty,
                request.AcademicYear ?? string.Empty,
                request.ConfessionFather ?? string.Empty,
                request.Notes ?? string.Empty,
                request.Gender,
                request.RequestedByUserId,
                request.BirthDate
            );

            var createResult = await _studentCommandsService.Create(createReq);
            if (createResult.Status != ServiceResultStatus.Success)
                return createResult;

            request.Status = AssignmentRequestStatus.Approved;
            request.ProcessedAt = DateTime.UtcNow;
            request.ProcessedByUserId = adminUserId;
            _repo.Update(request);
            await _unitOfWork.SaveChangesAsync();
            await _activityLogger.LogAsync("موافقة على طلب إضافة مخدوم", $"تمت الموافقة على إضافة مخدوم: {request.FullName}");
            return ServiceResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving addition request {RequestId}", requestId);
            return ServiceResult.Failure("حدث خطأ أثناء الموافقة على الطلب.");
        }
    }

    public async Task<ServiceResult> Reject(Guid requestId, Guid adminUserId)
    {
        try
        {
            var request = await _repo.GetById(requestId);
            if (request == null)
                return ServiceResult.Failure("الطلب غير موجود.");
            if (request.Status != AssignmentRequestStatus.Pending)
                return ServiceResult.Failure("تمت معالجة هذا الطلب مسبقاً.");

            request.Status = AssignmentRequestStatus.Rejected;
            request.ProcessedAt = DateTime.UtcNow;
            request.ProcessedByUserId = adminUserId;
            _repo.Update(request);
            await _unitOfWork.SaveChangesAsync();
            return ServiceResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rejecting addition request {RequestId}", requestId);
            return ServiceResult.Failure("حدث خطأ أثناء رفض الطلب.");
        }
    }

    private static StudentAdditionRequestDto MapToDto(StudentAdditionRequest r)
    {
        return new StudentAdditionRequestDto(
            r.Id,
            r.FullName,
            r.Phone,
            r.SecondaryPhone,
            r.Area,
            r.College,
            r.AcademicYear,
            r.Notes,
            (int)r.Gender,
            r.RequestedByUserId,
            r.RequestedByUser?.FullName ?? "",
            r.RequestedAt,
            r.Status.ToString()
        );
    }
}
