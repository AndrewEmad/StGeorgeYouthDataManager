using Microsoft.Extensions.Logging;
using YouthDataManager.Domain.Entities;
using YouthDataManager.Domain.Repositories.Tracking;
using YouthDataManager.Shared.Service.Abstractions;
using YouthDataManager.Students.Service.Abstractions.Commands;

namespace YouthDataManager.Students.Service.Implementations.Commands;

public class AttendanceService : IAttendanceService
{
    private readonly IStudentAttendanceRepository _studentAttendanceRepo;
    private readonly IServantAttendanceRepository _servantAttendanceRepo;
    private readonly IStudentRepository _studentRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AttendanceService> _logger;

    public AttendanceService(
        IStudentAttendanceRepository studentAttendanceRepo,
        IServantAttendanceRepository servantAttendanceRepo,
        IStudentRepository studentRepo,
        IUnitOfWork unitOfWork,
        ILogger<AttendanceService> logger)
    {
        _studentAttendanceRepo = studentAttendanceRepo;
        _servantAttendanceRepo = servantAttendanceRepo;
        _studentRepo = studentRepo;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<ServiceResult> RecordStudentAttendanceAsync(DateTime date, IReadOnlyList<Guid> studentIds)
    {
        if (studentIds == null || studentIds.Count == 0)
            return ServiceResult.Failure("يجب اختيار مخدوم واحد على الأقل.");
        var dateOnly = date.Date;
        try
        {
            var distinctIds = studentIds.Distinct().ToList();
            foreach (var studentId in distinctIds)
            {
                _studentAttendanceRepo.Add(new StudentAttendance
                {
                    Id = Guid.NewGuid(),
                    StudentId = studentId,
                    Date = dateOnly
                });
            }
            await _unitOfWork.SaveChangesAsync();
            foreach (var studentId in distinctIds)
            {
                var student = await _studentRepo.GetById(studentId);
                if (student != null && (student.LastAttendanceDate == null || student.LastAttendanceDate.Value < dateOnly))
                {
                    student.LastAttendanceDate = dateOnly;
                    _studentRepo.Update(student);
                }
            }
            await _unitOfWork.SaveChangesAsync();
            return ServiceResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording student attendance for date {Date}", dateOnly);
            return ServiceResult.Failure("حدث خطأ أثناء تسجيل الحضور.");
        }
    }

    public async Task<ServiceResult> RecordServantAttendanceAsync(DateTime date, IReadOnlyList<Guid> servantIds)
    {
        if (servantIds == null || servantIds.Count == 0)
            return ServiceResult.Failure("يجب اختيار خادم واحد على الأقل.");
        var dateOnly = date.Date;
        try
        {
            var distinctIds = servantIds.Distinct().ToList();
            foreach (var servantId in distinctIds)
            {
                _servantAttendanceRepo.Add(new ServantAttendance
                {
                    Id = Guid.NewGuid(),
                    ServantId = servantId,
                    Date = dateOnly
                });
            }
            await _unitOfWork.SaveChangesAsync();
            return ServiceResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording servant attendance for date {Date}", dateOnly);
            return ServiceResult.Failure("حدث خطأ أثناء تسجيل حضور الخدام.");
        }
    }
}
