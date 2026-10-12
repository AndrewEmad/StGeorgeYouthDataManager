using YouthDataManager.Shared.Service.Abstractions;
using YouthDataManager.Students.Service.Abstractions.Commands;
namespace YouthDataManager.Students.Service.Implementations.Commands;
// Compatibility adapter. New clients mark draft session rosters and explicitly finalize them.
public sealed class AttendanceService(IMeetingService meetings) : IAttendanceService
{
    public Task<ServiceResult> RecordStudentAttendanceAsync(DateTime date,IReadOnlyList<Guid> ids) => Record(date,ids,"Student");
    public Task<ServiceResult> RecordServantAttendanceAsync(DateTime date,IReadOnlyList<Guid> ids) => Record(date,ids,"Servant");
    private async Task<ServiceResult> Record(DateTime date,IReadOnlyList<Guid> ids,string kind)
    {
        var session=await meetings.Start(date);
        if (session.Status!=ServiceResultStatus.Success || session.Data==null) return ServiceResult.Failure(session.Message ?? "Meeting unavailable.");
        var mark=await meetings.Mark(session.Data.Id,kind,ids,true,"Legacy client attendance marking");
        if (mark.Status!=ServiceResultStatus.Success) return mark;
        return session.Data.Status=="Draft" ? await meetings.ChangeStatus(session.Data.Id,"Finalized",null) : ServiceResult.Success();
    }
}
