using YouthDataManager.Shared.Service.Abstractions;

namespace YouthDataManager.Students.Service.Abstractions.Commands;

public interface IAttendanceService
{
    Task<ServiceResult> RecordStudentAttendanceAsync(DateTime date, IReadOnlyList<Guid> studentIds);
    Task<ServiceResult> RecordServantAttendanceAsync(DateTime date, IReadOnlyList<Guid> servantIds);
}
