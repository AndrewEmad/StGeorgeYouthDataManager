using Microsoft.EntityFrameworkCore;
using YouthDataManager.Domain.Entities;
using YouthDataManager.Domain.Enums;
using YouthDataManager.Domain.Utilities;
using YouthDataManager.Shared.Service.Abstractions;
using YouthDataManager.Students.Service.Abstractions.Commands;
using YouthDataManager.Students.Service.Abstractions.DTOs;

namespace YouthDataManager.Students.Service.Implementations.Commands;

public sealed class StudentCommandsService(IApplicationStore store, ICallerContext caller) : IStudentCommandsService
{
    private static ServiceResult Fail(string message, string code = "Validation") => ServiceResult.Failure(message, new ServiceError(code));
    private Task<ServiceResult> Operation(Func<Task<ServiceResult>> action) => store.Transaction(action, r => r.Status == ServiceResultStatus.Success);

    public Task<ServiceResult<Guid>> Create(CreateStudentRequest r) => store.Transaction(async () =>
    {
        if (!caller.IsPrivileged) return ServiceResult<Guid>.Failure("Approval required.", new ServiceError("Forbidden"));
        var error = StudentFields.Validate(r.FullName,r.Phone,r.SecondaryPhone,r.Address,r.Area,r.College,r.AcademicYear,r.ConfessionFather,r.Notes,r.Gender,r.BirthDate,r.BirthMonth);
        if (error != null) return ServiceResult<Guid>.Failure(error);
        if (r.ServantId.HasValue && !await EligibleUsers.Query(store).AnyAsync(u => u.Id == r.ServantId, caller.CancellationToken)) return ServiceResult<Guid>.Failure("Assignee is inactive or ineligible.");
        var student = new Student { Id = Guid.NewGuid(), FullName = r.FullName.Trim(), NormalizedFullName = ArabicNormalizer.Normalize(r.FullName.Trim()),
            Phone = r.Phone.Trim(), SecondaryPhone = r.SecondaryPhone?.Trim(), Address = r.Address?.Trim() ?? "", Area = r.Area?.Trim() ?? "",
            College = r.College?.Trim() ?? "", AcademicYear = r.AcademicYear?.Trim() ?? "", ConfessionFather = r.ConfessionFather?.Trim() ?? "",
            Notes = r.Notes?.Trim() ?? "", Gender = r.Gender, ServantId = r.ServantId, BirthDate = r.BirthDate?.Date, BirthMonth = r.BirthDate?.Month ?? r.BirthMonth };
        store.Add(student);
        return ServiceResult<Guid>.Success(student.Id);
    }, result => result.Status == ServiceResultStatus.Success);

    public Task<ServiceResult> Update(UpdateStudentRequest r, Guid? currentUserId, bool isAdmin) => Operation(async () =>
    {
        var s = await store.Query<Student>().SingleOrDefaultAsync(s => s.Id == r.Id, caller.CancellationToken);
        if (s == null) return Fail("Student not found.","NotFound");
        if (!caller.IsPrivileged && s.ServantId != caller.UserId) return Fail("Not authorized.","Forbidden");
        if(caller.ExpectedVersion!=null && caller.ExpectedVersion!=Convert.ToBase64String(s.Version))return Fail("Student changed. Refresh before retrying.","Conflict");
        var fullName=r.FullName.IsPresent ? r.FullName.Value ?? "" : s.FullName;
        var phone=r.Phone.IsPresent ? r.Phone.Value ?? "" : s.Phone;
        var address=r.Address.IsPresent ? r.Address.Value?.Trim() ?? "" : s.Address;
        var area=r.Area.IsPresent ? r.Area.Value?.Trim() ?? "" : s.Area;
        var college=r.College.IsPresent ? r.College.Value?.Trim() ?? "" : s.College;
        var year=r.AcademicYear.IsPresent ? r.AcademicYear.Value?.Trim() ?? "" : s.AcademicYear;
        var father=r.ConfessionFather.IsPresent ? r.ConfessionFather.Value?.Trim() ?? "" : s.ConfessionFather;
        var gender=r.Gender.IsPresent ? r.Gender.Value : s.Gender;
        if(!gender.HasValue)return Fail("Gender is required.");
        var birth = r.BirthDate.IsPresent ? r.BirthDate.Value?.Date : s.BirthDate;
        var month = birth?.Month ?? (r.BirthMonth.IsPresent ? r.BirthMonth.Value : s.BirthMonth);
        var secondary = r.SecondaryPhone.IsPresent ? r.SecondaryPhone.Value?.Trim() : s.SecondaryPhone;
        var notes = r.Notes.IsPresent ? r.Notes.Value?.Trim() ?? "" : s.Notes;
        var error = StudentFields.Validate(fullName,phone,secondary,address,area,college,year,father,notes,gender.Value,birth,month);
        if (error != null) return Fail(error);
        if (r.ServantId.IsPresent && r.ServantId.Value != s.ServantId)
        {
            if (!caller.IsPrivileged) return Fail("Reassignment requires approval.","Forbidden");
            if (r.ServantId.Value.HasValue && !await EligibleUsers.Query(store).AnyAsync(u => u.Id == r.ServantId.Value, caller.CancellationToken)) return Fail("Assignee is inactive or ineligible.");
            s.ServantId = r.ServantId.Value;
            await TransferTasks(s.Id,s.ServantId);
        }
        s.FullName = fullName.Trim(); s.NormalizedFullName = ArabicNormalizer.Normalize(s.FullName); s.Phone = phone.Trim(); s.SecondaryPhone = secondary;
        s.Address = address; s.Area = area; s.College = college;
        s.AcademicYear = year; s.ConfessionFather = father; s.Notes = notes; s.Gender = gender.Value;
        s.BirthDate = birth; s.BirthMonth = month; s.UpdatedAt = DateTime.UtcNow;
        store.Add(new StudentEditLog { Id = Guid.NewGuid(),StudentId=s.Id,UpdatedAt=s.UpdatedAt.Value,UpdatedByUserId=caller.UserId ?? Guid.Empty,UpdatedByUserName=caller.UserId?.ToString() ?? "System",Details="Student fields updated; see structured audit for before/after values." });
        return ServiceResult.Success();
    });

    public Task<ServiceResult> Delete(Guid id) => Operation(async () =>
    {
        if (!caller.IsPrivileged) return Fail("Approval required.","Forbidden");
        var s = await store.Query<Student>(true,true).SingleOrDefaultAsync(s => s.Id == id, caller.CancellationToken);
        if (s == null) return Fail("Student not found.","NotFound");
        if(s.IsDeleted)return ServiceResult.Success();
        if(string.IsNullOrWhiteSpace(caller.Reason) || caller.Reason.Length>1000)return Fail("An archival reason (1–1000 characters) is required.");
        if(caller.ExpectedVersion!=null && caller.ExpectedVersion!=Convert.ToBase64String(s.Version))return Fail("Student changed. Refresh before archiving.","Conflict");
        s.IsDeleted = true; s.ServantId = null; s.UpdatedAt = DateTime.UtcNow;
        await ArchiveWork(store,id,caller);
        return ServiceResult.Success();
    });

    public Task<ServiceResult> AssignToServant(Guid id, Guid servantId) => Operation(async () =>
    {
        if (!caller.IsPrivileged) return Fail("Approval required.","Forbidden");
        if (!await EligibleUsers.Query(store).AnyAsync(u => u.Id == servantId, caller.CancellationToken)) return Fail("Assignee is inactive or ineligible.");
        var s = await store.Query<Student>().SingleOrDefaultAsync(s => s.Id == id, caller.CancellationToken);
        if (s == null) return Fail("Student not found.","NotFound");
        s.ServantId = servantId; s.UpdatedAt = DateTime.UtcNow;
        await TransferTasks(id,servantId);
        return ServiceResult.Success();
    });
    public Task<ServiceResult> UnassignFromServant(Guid id) => Operation(async () =>
    {
        if (!caller.IsPrivileged) return Fail("Approval required.","Forbidden");
        var s = await store.Query<Student>().SingleOrDefaultAsync(s => s.Id == id, caller.CancellationToken);
        if (s == null) return Fail("Student not found.","NotFound");
        s.ServantId = null; s.UpdatedAt = DateTime.UtcNow;
        await TransferTasks(id,null);
        return ServiceResult.Success();
    });
    private async Task TransferTasks(Guid id, Guid? owner)
    {
        foreach (var task in await store.Query<FollowUpTask>().Where(t => t.StudentId == id && (t.Status == "Open" || t.Status == "PendingReview")).ToListAsync(caller.CancellationToken)) task.AssigneeId = owner;
    }
    public static async Task ArchiveWork(IApplicationStore store, Guid id,ICallerContext caller)
    {
        foreach (var t in await store.Query<FollowUpTask>().Where(t => t.StudentId == id && (t.Status == "Open" || t.Status == "PendingReview")).ToListAsync(caller.CancellationToken)) { t.Status="Cancelled"; t.Reason="Student archived"; }
        foreach (var r in await store.Query<StudentAssignmentRequest>().Where(r => r.StudentId==id && r.Status==AssignmentRequestStatus.Pending).ToListAsync(caller.CancellationToken)) { r.Status=AssignmentRequestStatus.Cancelled; r.ProcessedAt=caller.UtcNow; r.ProcessedByUserId=caller.UserId;r.ProcessingReason="Student archived"; }
        foreach (var r in await store.Query<StudentRemovalRequest>().Where(r => r.StudentId==id && r.Status==RemovalRequestStatus.Pending).ToListAsync(caller.CancellationToken)) { r.Status=RemovalRequestStatus.Cancelled; r.ProcessedAt=caller.UtcNow; r.ProcessedByUserId=caller.UserId;r.ProcessingReason="Student archived"; }
    }
}
