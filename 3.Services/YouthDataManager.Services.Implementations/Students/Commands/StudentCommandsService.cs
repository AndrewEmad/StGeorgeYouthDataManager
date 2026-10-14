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
            Phone = r.Phone.Trim(), SecondaryPhone = r.SecondaryPhone?.Trim(), Address = r.Address?.Trim() ?? "", Area = StudentAreas.Canonical(r.Area)!,
            College = r.College?.Trim() ?? "", AcademicYear = AcademicYears.Canonical(r.AcademicYear)!, ConfessionFather = r.ConfessionFather?.Trim() ?? "",
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
        if(r.Area.IsPresent){if(area.Length>StudentFields.AreaLimit)return Fail("المنطقة يجب ألا تتجاوز 100 حرف.");area=StudentAreas.Canonical(area);}
        var areaChanged=r.Area.IsPresent && area!=s.Area;
        var college=r.College.IsPresent ? r.College.Value?.Trim() ?? "" : s.College;
        var year=r.AcademicYear.IsPresent ? r.AcademicYear.Value?.Trim() ?? "" : s.AcademicYear;
        var yearChanged=r.AcademicYear.IsPresent && year!=s.AcademicYear;
        if(yearChanged){var canonical=AcademicYears.Canonical(year);if(canonical==null)return Fail("اختر سنة دراسية من القائمة المحددة.");year=canonical;}
        var father=r.ConfessionFather.IsPresent ? r.ConfessionFather.Value?.Trim() ?? "" : s.ConfessionFather;
        var gender=r.Gender.IsPresent ? r.Gender.Value : s.Gender;
        if(!gender.HasValue)return Fail("Gender is required.");
        var birth = r.BirthDate.IsPresent ? r.BirthDate.Value?.Date : s.BirthDate;
        var month = birth?.Month ?? (r.BirthMonth.IsPresent ? r.BirthMonth.Value : s.BirthMonth);
        var secondary = r.SecondaryPhone.IsPresent ? r.SecondaryPhone.Value?.Trim() : s.SecondaryPhone;
        var notes = r.Notes.IsPresent ? r.Notes.Value?.Trim() ?? "" : s.Notes;
        var error = StudentFields.Validate(fullName,phone,secondary,address,areaChanged ? area : null,college,yearChanged ? year : null,father,notes,gender.Value,birth,month);
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

    public Task<ServiceResult<BulkAcademicYearResult>> UpdateAcademicYears(BulkAcademicYearRequest request)=>UpdateRosterField(request.Students,request.AcademicYear,false,
        rows=>new BulkAcademicYearResult(rows.Select(o=>new BulkAcademicYearOutcome(o.StudentId,o.Success,o.Error,o.Code,o.FullName,o.Value,o.Version,o.Changed)).ToList()));
    public Task<ServiceResult<BulkAreaResult>> UpdateAreas(BulkAreaRequest request)=>UpdateRosterField(request.Students,request.Area,true,
        rows=>new BulkAreaResult(rows.Select(o=>new BulkAreaOutcome(o.StudentId,o.Success,o.Error,o.Code,o.FullName,o.Value,o.Version,o.Changed)).ToList()));
    private sealed record BulkFieldOutcome(Guid StudentId,bool Success,string? Error=null,string? Code=null,string? FullName=null,string? Value=null,string? Version=null,bool Changed=false);
    private Task<ServiceResult<T>> UpdateRosterField<T>(List<SelectedStudentVersion>? selected,string? value,bool isArea,Func<IReadOnlyList<BulkFieldOutcome>,T> createResult)
    {
        var label=isArea ? "المنطقة" : "السنة الدراسية";
        return store.Transaction(async () =>
        {
            if (!caller.IsPrivileged) return ServiceResult<T>.Failure($"غير مسموح بتغيير {label} لمجموعة.", new ServiceError("Forbidden"));
            var error = isArea ? StudentFields.ValidateArea(value) : StudentFields.ValidateAcademicYear(value);
            if (error != null) return ServiceResult<T>.Failure(error);
            static bool ValidVersion(string? version)
            {
                Span<byte> bytes = stackalloc byte[8];
                return version != null && Convert.TryFromBase64String(version, bytes, out var length) && length == 8;
            }
            if (selected == null || selected.Count is < 1 or > 5000 || selected.Any(s => s == null || s.StudentId == Guid.Empty || !ValidVersion(s.Version)) || selected.Select(s => s.StudentId).Distinct().Count() != selected.Count)
                return ServiceResult<T>.Failure("اختر من 1 إلى 5000 مخدوم مع أحدث بياناتهم.");
            var target = (isArea ? StudentAreas.Canonical(value) : AcademicYears.Canonical(value))!;
            var ids = selected.Select(s => s.StudentId).ToArray();
            // Load the selected active roster once. Serializable transaction + row versions
            // prevent concurrent changes while successful rows and audit records are saved together.
            var students = await store.Query<Student>().Where(s => ids.Contains(s.Id)).ToDictionaryAsync(s => s.Id, caller.CancellationToken);
            var actorName = await store.Query<ApplicationUser>(false).Where(u => u.Id == caller.UserId).Select(u => u.FullName).SingleOrDefaultAsync(caller.CancellationToken) ?? "";
            var outcomes = new List<BulkFieldOutcome>();
            foreach (var item in selected)
            {
                caller.CancellationToken.ThrowIfCancellationRequested();
                if (!students.TryGetValue(item.StudentId, out var student))
                {
                    outcomes.Add(new(item.StudentId, false, "المخدوم غير موجود أو تمت أرشفته.", "NotFound"));
                    continue;
                }
                var version = Convert.ToBase64String(student.Version);
                // A lost-response retry is harmless when the requested value is already saved.
                if ((isArea ? student.Area : student.AcademicYear) == target)
                {
                    outcomes.Add(new(student.Id, true, FullName: student.FullName, Value: target, Version: version));
                    continue;
                }
                if (version != item.Version)
                {
                    outcomes.Add(new(student.Id, false, "تغيرت بيانات المخدوم. راجع القيمة المسجلة قبل إعادة المحاولة.", "Conflict", student.FullName, isArea ? student.Area : student.AcademicYear, version));
                    continue;
                }
                var previous = isArea ? student.Area : student.AcademicYear;
                if(isArea)student.Area=target;else student.AcademicYear=target;
                student.UpdatedAt = caller.UtcNow;
                store.Add(new StudentEditLog { Id = Guid.NewGuid(), StudentId = student.Id, UpdatedAt = caller.UtcNow,
                    UpdatedByUserId = caller.UserId ?? Guid.Empty, UpdatedByUserName = actorName,
                    Details = $"تغيير {label} من «{previous}» إلى «{target}»." });
                outcomes.Add(new(student.Id, true, FullName: student.FullName, Value: target, Changed: true));
            }
            return ServiceResult<T>.Success(createResult(outcomes));
        }, result => result.Status == ServiceResultStatus.Success);
    }
    public static async Task ArchiveWork(IApplicationStore store, Guid id,ICallerContext caller)
    {
        foreach (var t in await store.Query<FollowUpTask>().Where(t => t.StudentId == id && (t.Status == "Open" || t.Status == "PendingReview")).ToListAsync(caller.CancellationToken)) { t.Status="Cancelled"; t.Reason="Student archived"; }
        foreach (var r in await store.Query<StudentAssignmentRequest>().Where(r => r.StudentId==id && r.Status==AssignmentRequestStatus.Pending).ToListAsync(caller.CancellationToken)) { r.Status=AssignmentRequestStatus.Cancelled; r.ProcessedAt=caller.UtcNow; r.ProcessedByUserId=caller.UserId;r.ProcessingReason="Student archived"; }
        foreach (var r in await store.Query<StudentRemovalRequest>().Where(r => r.StudentId==id && r.Status==RemovalRequestStatus.Pending).ToListAsync(caller.CancellationToken)) { r.Status=RemovalRequestStatus.Cancelled; r.ProcessedAt=caller.UtcNow; r.ProcessedByUserId=caller.UserId;r.ProcessingReason="Student archived"; }
    }
}
