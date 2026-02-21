using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using YouthDataManager.Domain.Entities;
using YouthDataManager.Domain.Repositories.Tracking;
using YouthDataManager.Shared.Service.Abstractions;
using YouthDataManager.Students.Service.Abstractions.Commands;
using YouthDataManager.Students.Service.Abstractions.DTOs;

namespace YouthDataManager.Students.Service.Implementations.Commands;

public class StudentCommandsService : IStudentCommandsService
{
    private readonly IStudentRepository _repository;
    private readonly IStudentEditLogRepository _editLogRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IActivityLogger _activityLogger;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<StudentCommandsService> _logger;

    public StudentCommandsService(
        IStudentRepository repository,
        IStudentEditLogRepository editLogRepository,
        IUnitOfWork unitOfWork,
        IActivityLogger activityLogger,
        IHttpContextAccessor httpContextAccessor,
        ILogger<StudentCommandsService> logger)
    {
        _repository = repository;
        _editLogRepository = editLogRepository;
        _unitOfWork = unitOfWork;
        _activityLogger = activityLogger;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public async Task<ServiceResult<Guid>> Create(CreateStudentRequest request)
    {
        try
        {
            var student = new Student
            {
                Id = Guid.NewGuid(),
                FullName = request.FullName,
                Phone = request.Phone,
                SecondaryPhone = request.SecondaryPhone,
                Address = request.Address ?? string.Empty,
                Area = request.Area ?? string.Empty,
                College = request.College ?? string.Empty,
                AcademicYear = request.AcademicYear ?? string.Empty,
                ConfessionFather = request.ConfessionFather ?? string.Empty,
                Notes = request.Notes ?? string.Empty,
                Gender = request.Gender,
                ServantId = request.ServantId,
                BirthDate = request.BirthDate,
                CreatedAt = DateTime.UtcNow
            };

            _repository.Add(student);
            await _unitOfWork.SaveChangesAsync();

            await _activityLogger.LogAsync("إضافة مخدوم", $"تم إضافة مخدوم جديد: {student.FullName}");

            return ServiceResult<Guid>.Success(student.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating student {StudentName}", request.FullName);
            return ServiceResult<Guid>.Failure("An error occurred while creating the student.");
        }
    }

    public async Task<ServiceResult> Update(UpdateStudentRequest request, Guid? currentUserId, bool isAdmin)
    {
        try
        {
            var student = await _repository.GetById(request.Id);
            if (student == null)
                return ServiceResult.Failure("Student not found", new ServiceError("NotFound", "Student ID does not exist"));

            if (!isAdmin)
            {
                if (!currentUserId.HasValue || student.ServantId != currentUserId.Value)
                    return ServiceResult.Failure("Not authorized to update this student.");
            }

            student.FullName = request.FullName;
            student.Phone = request.Phone;
            student.Address = request.Address ?? string.Empty;
            student.Area = request.Area ?? string.Empty;
            student.College = request.College ?? string.Empty;
            student.AcademicYear = request.AcademicYear ?? string.Empty;
            student.ConfessionFather = request.ConfessionFather ?? string.Empty;
            student.Gender = request.Gender;
            if (isAdmin)
                student.ServantId = request.ServantId;
            student.BirthDate = request.BirthDate;
            student.UpdatedAt = DateTime.UtcNow;

            _repository.Update(student);

            var userName = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.Name)?.Value
                ?? _httpContextAccessor.HttpContext?.User?.Identity?.Name
                ?? "غير معروف";
            var userId = currentUserId ?? Guid.Empty;
            _editLogRepository.Add(new StudentEditLog
            {
                Id = Guid.NewGuid(),
                StudentId = student.Id,
                UpdatedAt = student.UpdatedAt.Value,
                UpdatedByUserId = userId,
                UpdatedByUserName = userName,
                Details = "تحديث بيانات المخدوم"
            });

            await _unitOfWork.SaveChangesAsync();

            await _activityLogger.LogAsync("تحديث مخدوم", $"تم تحديث بيانات المخدوم: {student.FullName}");

            return ServiceResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating student {StudentId}", request.Id);
            return ServiceResult.Failure("An error occurred while updating the student.");
        }
    }

    public async Task<ServiceResult> Delete(Guid id)
    {
        try
        {
            var student = await _repository.GetById(id);
            if (student == null)
                return ServiceResult.Failure("Student not found");

            _repository.Remove(student);
            await _unitOfWork.SaveChangesAsync();

            await _activityLogger.LogAsync("حذف مخدوم", $"تم حذف المخدوم: {student.FullName}");

            return ServiceResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting student {StudentId}", id);
            return ServiceResult.Failure("An error occurred while deleting the student.");
        }
    }

    public async Task<ServiceResult> AssignToServant(Guid studentId, Guid servantId)
    {
        try
        {
            var student = await _repository.GetById(studentId);
            if (student == null)
                return ServiceResult.Failure("Student not found");

            student.ServantId = servantId;
            student.UpdatedAt = DateTime.UtcNow;

            _repository.Update(student);
            await _unitOfWork.SaveChangesAsync();

            await _activityLogger.LogAsync("تعيين خادم", $"تم تعيين خادم للمخدوم: {student.FullName}");

            return ServiceResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning student {StudentId} to servant {ServantId}", studentId, servantId);
            return ServiceResult.Failure("An error occurred while assigning the servant.");
        }
    }

    public async Task<ServiceResult> UnassignFromServant(Guid studentId)
    {
        try
        {
            var student = await _repository.GetById(studentId);
            if (student == null)
                return ServiceResult.Failure("Student not found");

            student.ServantId = null;
            student.IsDeleted = true;
            student.UpdatedAt = DateTime.UtcNow;

            _repository.Update(student);
            await _unitOfWork.SaveChangesAsync();

            await _activityLogger.LogAsync("إزالة مخدوم من الخادم", $"تم إزالة المخدوم: {student.FullName}");

            return ServiceResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unassigning student {StudentId}", studentId);
            return ServiceResult.Failure("An error occurred while unassigning the student.");
        }
    }
}
