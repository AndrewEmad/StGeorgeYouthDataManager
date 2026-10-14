using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using YouthDataManager.Domain.Entities;
using YouthDataManager.Domain.Enums;
using YouthDataManager.Domain.Utilities;
using YouthDataManager.Shared.Service.Abstractions;
using YouthDataManager.Students.Service.Abstractions.Queries;
using YouthDataManager.Students.Service.Abstractions.DTOs;

namespace YouthDataManager.Students.Service.Implementations.Queries;

public sealed class StudentQueriesService(IApplicationStore store,ICallerContext caller) : IStudentQueriesService
{
    private IQueryable<Student> Assigned() => store.Query<Student>(false).Where(s=>caller.IsPrivileged || s.ServantId==caller.UserId);
    public async Task<ServiceResult<StudentDto>> GetById(Guid id)
    {
        var detail=caller.IsPrivileged ? store.Query<Student>(false,true) : Assigned();
        var result=await detail.Where(s=>s.Id==id).Select(Selector()).SingleOrDefaultAsync(caller.CancellationToken);
        return result==null ? ServiceResult<StudentDto>.Failure("Student unavailable.") : ServiceResult<StudentDto>.Success(result);
    }
    public async Task<ServiceResult<IEnumerable<StudentDto>>> GetAll() => ServiceResult<IEnumerable<StudentDto>>.Success(await Assigned().OrderBy(s=>s.FullName).ThenBy(s=>s.Id).Select(Selector()).ToListAsync(caller.CancellationToken));
    public async Task<ServiceResult<IEnumerable<StudentDto>>> GetByServantId(Guid id)
    {
        if (!caller.IsPrivileged && id!=caller.UserId) return ServiceResult<IEnumerable<StudentDto>>.Failure("Not authorized.",new ServiceError("Forbidden"));
        return ServiceResult<IEnumerable<StudentDto>>.Success(await Assigned().Where(s=>s.ServantId==id).OrderBy(s=>s.FullName).ThenBy(s=>s.Id).Select(Selector()).ToListAsync(caller.CancellationToken));
    }
    private static bool ValidPage(int page,int size) => page>0 && size is >=1 and <=100 && (long)(page-1)*size<=int.MaxValue;
    private IQueryable<Student> Filter(IQueryable<Student> q,string? search,string? area,string? year,int? gender)
    {
        if (!string.IsNullOrWhiteSpace(search)) { var term=ArabicNormalizer.Normalize(search.Trim()); q=q.Where(s=>s.NormalizedFullName.Contains(term) || s.Phone.Contains(search.Trim())); }
        if (area!=null) q=area=="__missing__" ? q.Where(s=>s.Area==null || s.Area.Trim()=="") : q.Where(s=>s.Area==area);
        if (year!=null) q=year=="__missing__" ? q.Where(s=>s.AcademicYear==null || s.AcademicYear.Trim()=="") : q.Where(s=>s.AcademicYear==year);
        if (gender.HasValue) q=q.Where(s=>(int)s.Gender==gender.Value);
        return q;
    }
    public async Task<ServiceResult<PagedResult<StudentDto>>> GetPaged(StudentListFilter f,int page,int pageSize)
    {
        if (!ValidPage(page,pageSize) || (f.Gender.HasValue && !Enum.IsDefined(typeof(Gender),f.Gender.Value)) || f.BirthMonth is <0 or >12) return ServiceResult<PagedResult<StudentDto>>.Failure("Invalid filters or pagination.");
        var q=Filter(Assigned(),f.Search,f.Area,f.AcademicYear,f.Gender);
        if (f.ServantId.HasValue) q=q.Where(s=>s.ServantId==f.ServantId);
        if (f.HasServant.HasValue) q=f.HasServant.Value ? q.Where(s=>s.ServantId!=null) : q.Where(s=>s.ServantId==null);
        if (f.ExcludeStudentIds!=null) { var ids=f.ExcludeStudentIds.ToArray();q=q.Where(s=>!ids.Contains(s.Id)); }
        if (f.BirthMonth.HasValue) q=f.BirthMonth==0 ? q.Where(s=>s.BirthDate==null && s.BirthMonth==null) : q.Where(s=>(s.BirthDate.HasValue ? s.BirthDate.Value.Month : s.BirthMonth)==f.BirthMonth);
        var key=(f.SortBy ?? "fullname").ToLowerInvariant(); var desc=f.SortDesc==true;
        if (key is not ("fullname" or "area" or "academicyear" or "phone" or "createdat" or "servantname" or "lastattendancedate" or "orderindex")) return ServiceResult<PagedResult<StudentDto>>.Failure("Invalid sort key.");
        IOrderedQueryable<Student> ordered=key switch {
            "createdat" => desc ? q.OrderByDescending(s=>s.CreatedAt) : q.OrderBy(s=>s.CreatedAt),
            "area" => desc ? q.OrderByDescending(s=>s.Area) : q.OrderBy(s=>s.Area),
            "academicyear" => desc ? q.OrderByDescending(s=>s.AcademicYear) : q.OrderBy(s=>s.AcademicYear),
            "phone" => desc ? q.OrderByDescending(s=>s.Phone) : q.OrderBy(s=>s.Phone),
            "servantname" => desc ? q.OrderByDescending(s=>s.Servant!.FullName) : q.OrderBy(s=>s.Servant!.FullName),
            "lastattendancedate" => desc ? q.OrderByDescending(s=>s.LastAttendanceDate) : q.OrderBy(s=>s.LastAttendanceDate),
            "orderindex" => desc ? q.OrderByDescending(s=>s.OrderIndex) : q.OrderBy(s=>s.OrderIndex),
            _ => desc ? q.OrderByDescending(s=>s.FullName) : q.OrderBy(s=>s.FullName) };
        return ServiceResult<PagedResult<StudentDto>>.Success(new(await ordered.ThenBy(s=>s.Id).Skip((page-1)*pageSize).Take(pageSize).Select(Selector()).ToListAsync(caller.CancellationToken),await q.CountAsync(caller.CancellationToken),page,pageSize));
    }
    public Task<ServiceResult<IEnumerable<string>>> GetDistinctAreas() => Task.FromResult(ServiceResult<IEnumerable<string>>.Success(StudentAreas.Values));
    public async Task<ServiceResult<IEnumerable<string>>> GetDistinctAcademicYears() => ServiceResult<IEnumerable<string>>.Success(await store.Query<Student>(false).Select(s=>s.AcademicYear).Distinct().OrderBy(s=>s).ToListAsync(caller.CancellationToken));
    public async Task<ServiceResult<IEnumerable<StudentEditLogDto>>> GetEditHistory(Guid id)
    {
        if (!await Assigned().AnyAsync(s=>s.Id==id, caller.CancellationToken)) return ServiceResult<IEnumerable<StudentEditLogDto>>.Failure("Not authorized.",new ServiceError("Forbidden"));
        var rows=await store.Query<StudentEditLog>(false,true).Where(e=>e.StudentId==id).OrderByDescending(e=>e.UpdatedAt).ThenBy(e=>e.Id).Select(e=>new StudentEditLogDto(e.Id,e.StudentId,e.UpdatedAt,e.UpdatedByUserId,e.UpdatedByUserName,e.Details)).ToListAsync(caller.CancellationToken);
        return ServiceResult<IEnumerable<StudentEditLogDto>>.Success(rows);
    }
    public async Task<ServiceResult<PagedResult<UnassignedStudentForServantDto>>> GetUnassignedForServant(Guid id,int page,int size)
    {
        if (!ValidPage(page,size)) return ServiceResult<PagedResult<UnassignedStudentForServantDto>>.Failure("Invalid pagination.");
        var q=store.Query<Student>(false).Where(s=>s.ServantId==null);
        var pending=store.Query<StudentAssignmentRequest>(false).Where(r=>r.RequestedByUserId==id && r.Status==AssignmentRequestStatus.Pending);
        var rows=await q.OrderBy(s=>s.FullName).ThenBy(s=>s.Id).Skip((page-1)*size).Take(size).Select(s=>new {Student=new StudentDiscoveryDto(s.Id,s.FullName,s.Area,s.College,s.AcademicYear),Pending=pending.Any(r=>r.StudentId==s.Id)}).ToListAsync(caller.CancellationToken);
        return ServiceResult<PagedResult<UnassignedStudentForServantDto>>.Success(new(rows.Select(r=>new UnassignedStudentForServantDto(r.Student,r.Pending)).ToList(),await q.CountAsync(caller.CancellationToken),page,size));
    }
    public async Task<ServiceResult<PagedResult<ServantStudentPageItemDto>>> GetPagedForServant(Guid id,int page,int size,string? search,string? area=null,string? academicYear=null,int? gender=null,string? segment=null)
    {
        if (segment is not (null or "Assigned" or "Unassigned") || !ValidPage(page,size) || (!caller.IsPrivileged && id!=caller.UserId)) return ServiceResult<PagedResult<ServantStudentPageItemDto>>.Failure("Invalid request.");
        var pending=store.Query<StudentAssignmentRequest>(false).Where(r=>r.RequestedByUserId==id && r.Status==AssignmentRequestStatus.Pending);
        var q=Filter(store.Query<Student>(false).Where(s=>s.ServantId==id || s.ServantId==null),search,area,academicYear,gender);
        if(segment=="Assigned")q=q.Where(s=>s.ServantId==id);
        else if(segment=="Unassigned")q=q.Where(s=>s.ServantId==null);
        var keys=await q.OrderBy(s=>s.ServantId==id ? 0 : pending.Any(r=>r.StudentId==s.Id) ? 1 : 2).ThenBy(s=>s.FullName).ThenBy(s=>s.Id).Skip((page-1)*size).Take(size)
            .Select(s=>new {s.Id,s.ServantId,s.FullName,s.Area,s.College,s.AcademicYear,Pending=pending.Any(r=>r.StudentId==s.Id)}).ToListAsync(caller.CancellationToken);
        var assignedIds=keys.Where(s=>s.ServantId==id).Select(s=>s.Id).ToArray();
        var assigned=await q.Where(s=>assignedIds.Contains(s.Id)).Select(Selector()).ToDictionaryAsync(s=>s.Id, caller.CancellationToken);
        var rows=keys.Select(s=>new ServantStudentPageItemDto(s.ServantId==id ? assigned[s.Id] : new StudentDiscoveryDto(s.Id,s.FullName,s.Area,s.College,s.AcademicYear),s.ServantId==id ? "Assigned" : s.Pending ? "Requested" : "Unassigned")).ToList();
        return ServiceResult<PagedResult<ServantStudentPageItemDto>>.Success(new(rows,await q.CountAsync(caller.CancellationToken),page,size));
    }
    private static Expression<Func<Student,StudentDto>> Selector() => s=>new StudentDto(s.Id,s.FullName,s.Phone,s.SecondaryPhone,s.Address,s.Area,s.College,s.AcademicYear,s.ConfessionFather,s.Notes,
        s.Servant==null ? null : s.Servant.FullName,s.ServantId,s.BirthDate,(int)s.Gender,
        s.CallLogs.Where(c=>!c.IsVoided).Select(c=>(DateTime?)c.CallDate).Concat(s.HomeVisits.Where(v=>!v.IsVoided).Select(v=>(DateTime?)v.VisitDate)).Max(),
        s.CallLogs.Where(c=>!c.IsVoided).Select(c=>new {Date=c.CallDate,c.Notes,c.Id}).Concat(s.HomeVisits.Where(v=>!v.IsVoided).Select(v=>new {Date=v.VisitDate,v.Notes,v.Id})).OrderByDescending(a=>a.Date).ThenBy(a=>a.Id).Select(a=>a.Notes).FirstOrDefault(),
        s.LastAttendanceDate,s.CreatedAt,s.UpdatedAt,s.PhotoPath,s.BirthDate.HasValue ? s.BirthDate.Value.Month : s.BirthMonth,s.Version,s.IsDeleted);
}
