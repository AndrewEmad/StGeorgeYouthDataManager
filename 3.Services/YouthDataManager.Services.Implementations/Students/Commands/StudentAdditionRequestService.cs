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
using YouthDataManager.Domain.Events;

namespace YouthDataManager.Students.Service.Implementations.Commands;

public class StudentAdditionRequestService : IStudentAdditionRequestService
{
    private readonly IStudentAdditionRequestRepository _repo;
    private readonly IStudentCommandsService _studentCommandsService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IActivityLogger _activityLogger;
    private readonly IApplicationStore _store;
    private readonly ICallerContext _caller;
    private readonly ILogger<StudentAdditionRequestService> _logger;
    private readonly IDomainEventDispatcher _dispatcher;

    public StudentAdditionRequestService(
        IStudentAdditionRequestRepository repo,
        IStudentCommandsService studentCommandsService,
        IUnitOfWork unitOfWork,
        IActivityLogger activityLogger, IApplicationStore store, ICallerContext caller,
        ILogger<StudentAdditionRequestService> logger,
        IDomainEventDispatcher dispatcher)
    {
        _repo = repo;
        _studentCommandsService = studentCommandsService;
        _unitOfWork = unitOfWork;
        _activityLogger = activityLogger; _store = store; _caller=caller;
        _logger = logger;
        _dispatcher = dispatcher;
    }

    public Task<ServiceResult> Approve(Guid requestId, Guid adminUserId, bool assignToRequestor) => _store.Transaction(() => ApproveCore(requestId, adminUserId, assignToRequestor), r => r.Status == ServiceResultStatus.Success);
    public Task<ServiceResult> Reject(Guid requestId, Guid adminUserId) => _store.Transaction(() => RejectCore(requestId, adminUserId), r => r.Status == ServiceResultStatus.Success);
    public Task<ServiceResult<Guid>> Create(CreateStudentAdditionRequestDto dto, Guid requestedByUserId) => _store.Transaction(() => CreateCore(dto, requestedByUserId), r => r.Status == ServiceResultStatus.Success);

    private async Task<ServiceResult<Guid>> CreateCore(CreateStudentAdditionRequestDto dto, Guid requestedByUserId)
    {
        if (!await EligibleUsers.Query(_store).AnyAsync(u=>u.Id==requestedByUserId, _caller.CancellationToken)) return ServiceResult<Guid>.Failure("Requester is inactive or ineligible.");
        try
        {
            if (string.IsNullOrWhiteSpace(dto.FullName) || string.IsNullOrWhiteSpace(dto.Phone))
                return ServiceResult<Guid>.Failure("الاسم ورقم الهاتف مطلوبان.");

            var validation=StudentFields.Validate(dto.FullName,dto.Phone,dto.SecondaryPhone,dto.Address,dto.Area,dto.College,dto.AcademicYear,dto.ConfessionFather,dto.Notes,(Gender)dto.Gender,dto.BirthDate,dto.BirthMonth);
            if (validation!=null) return ServiceResult<Guid>.Failure(validation);
            var key=Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(System.Text.Json.JsonSerializer.Serialize(new {Name=dto.FullName.Trim(),Phone=PhoneFields.Canonical(dto.Phone),Secondary=PhoneFields.Canonical(dto.SecondaryPhone),Address=dto.Address?.Trim(),Area=StudentAreas.Canonical(dto.Area),College=dto.College?.Trim(),Year=AcademicYears.Canonical(dto.AcademicYear),Father=dto.ConfessionFather?.Trim(),Notes=dto.Notes?.Trim(),dto.Gender,Birthday=dto.BirthDate?.Date,Month=dto.BirthDate?.Month ?? dto.BirthMonth}))));
            var duplicate=await _store.Query<StudentAdditionRequest>().FirstOrDefaultAsync(r=>r.RequestedByUserId==requestedByUserId && r.Status==AssignmentRequestStatus.Pending && r.RequestKey==key, _caller.CancellationToken);
            if (duplicate!=null) return ServiceResult<Guid>.Success(duplicate.Id);
            var request = new StudentAdditionRequest
            {
                Id = Guid.NewGuid(),
                RequestedByUserId = requestedByUserId,
                RequestedAt = DateTime.UtcNow,
                Status = AssignmentRequestStatus.Pending,
                RequestKey=key,
                FullName = dto.FullName.Trim(),
                Phone = dto.Phone.Trim(),
                SecondaryPhone = string.IsNullOrWhiteSpace(dto.SecondaryPhone) ? null : dto.SecondaryPhone.Trim(),
                Address = string.IsNullOrWhiteSpace(dto.Address) ? null : dto.Address.Trim(),
                Area = StudentAreas.Canonical(dto.Area),
                College = string.IsNullOrWhiteSpace(dto.College) ? null : dto.College.Trim(),
                AcademicYear = AcademicYears.Canonical(dto.AcademicYear),
                ConfessionFather = string.IsNullOrWhiteSpace(dto.ConfessionFather) ? null : dto.ConfessionFather.Trim(),
                Notes = string.IsNullOrWhiteSpace(dto.Notes) ? null : dto.Notes.Trim(),
                Gender = (Gender)dto.Gender,
                BirthMonth = dto.BirthDate?.Month ?? dto.BirthMonth,
                BirthDate = dto.BirthDate?.Date
            };

            _repo.Add(request);
            await _unitOfWork.SaveChangesAsync();
            await _activityLogger.LogAsync("طلب إضافة مخدوم", $"طلب الخادم إضافة مخدوم جديد: {request.FullName}");

            await _dispatcher.Dispatch(new StudentAdditionRequestCreatedEvent(request, requestedByUserId));

            return ServiceResult<Guid>.Success(request.Id);
        }
        catch (DbUpdateException) { throw; }
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
        catch (DbUpdateException) { throw; }
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
        catch (DbUpdateException) { throw; }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting addition requests for user {UserId}", userId);
            return ServiceResult<IEnumerable<StudentAdditionRequestDto>>.Failure("حدث خطأ أثناء تحميل الطلبات.");
        }
    }

    private async Task<ServiceResult> ApproveCore(Guid requestId, Guid adminUserId, bool assignToRequestor)
    {
        try
        {
            var request = await _repo.GetById(requestId);
            if (request == null)
                return ServiceResult.Failure("الطلب غير موجود.");
            if (request.Status == AssignmentRequestStatus.Approved) return ServiceResult.Success();
            if (_caller.ExpectedVersion!=null && _caller.ExpectedVersion!=Convert.ToBase64String(request.Version)) return ServiceResult.Failure("Request changed. Refresh before retrying.",new ServiceError("Conflict"));
            if (request.Status != AssignmentRequestStatus.Pending)
                return ServiceResult.Failure("تمت معالجة هذا الطلب مسبقاً.",new ServiceError("Conflict"));
            if (!await EligibleUsers.Query(_store).AnyAsync(u=>u.Id==request.RequestedByUserId, _caller.CancellationToken)) return ServiceResult.Failure("Requester is inactive or ineligible.",new ServiceError("Conflict"));


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
                assignToRequestor ? request.RequestedByUserId : null,
                request.BirthDate, request.BirthMonth
            );

            var createResult = await _studentCommandsService.Create(createReq);
            if (createResult.Status != ServiceResultStatus.Success)
                return createResult;

            request.CreatedStudentId=createResult.Data;
            request.Status = AssignmentRequestStatus.Approved;
            request.ProcessedAt = DateTime.UtcNow;
            request.ProcessedByUserId = adminUserId;
            request.ProcessingReason=_caller.Reason?.Trim();
            _repo.Update(request);
            await _unitOfWork.SaveChangesAsync();
            await _activityLogger.LogAsync("موافقة على طلب إضافة مخدوم", $"تمت الموافقة على إضافة مخدوم: {request.FullName}");

            await _dispatcher.Dispatch(new StudentAdditionRequestApprovedEvent(request, adminUserId, assignToRequestor));

            return ServiceResult.Success();
        }
        catch (DbUpdateException) { throw; }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving addition request {RequestId}", requestId);
            return ServiceResult.Failure("حدث خطأ أثناء الموافقة على الطلب.");
        }
    }

    private async Task<ServiceResult> RejectCore(Guid requestId, Guid adminUserId)
    {
        try
        {
            var request = await _repo.GetById(requestId);
            if (request == null)
                return ServiceResult.Failure("الطلب غير موجود.");
            if (_caller.ExpectedVersion!=null && _caller.ExpectedVersion!=Convert.ToBase64String(request.Version)) return ServiceResult.Failure("Request changed. Refresh before retrying.",new ServiceError("Conflict"));
            if (request.Status == AssignmentRequestStatus.Rejected) return ServiceResult.Success();
            if (request.Status != AssignmentRequestStatus.Pending)
                return ServiceResult.Failure("تمت معالجة هذا الطلب مسبقاً.",new ServiceError("Conflict"));

            request.Status = AssignmentRequestStatus.Rejected;
            request.ProcessedAt = DateTime.UtcNow;
            request.ProcessedByUserId = adminUserId;
            request.ProcessingReason=_caller.Reason?.Trim();
            _repo.Update(request);
            await _unitOfWork.SaveChangesAsync();

            await _dispatcher.Dispatch(new StudentAdditionRequestRejectedEvent(request, adminUserId));

            return ServiceResult.Success();
        }
        catch (DbUpdateException) { throw; }
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
            r.Status.ToString(),r.Address,r.ConfessionFather,r.BirthDate,r.ProcessedAt,r.ProcessedByUserId,r.CreatedStudentId,r.BirthMonth,r.ProcessingReason
        );
    }
}
