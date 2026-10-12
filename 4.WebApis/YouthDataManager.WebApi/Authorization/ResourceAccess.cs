using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using YouthDataManager.Domain.Entities;
using YouthDataManager.Shared.Service.Abstractions;

namespace YouthDataManager.WebApi.Authorization;

public sealed class ResourceAccess(IApplicationStore store, ICallerContext caller)
{
    public Guid UserId => caller.UserId ?? Guid.Empty;
    public bool Privileged => caller.IsPrivileged;
    public Task<bool> Student(Guid id, CancellationToken ct = default,bool historical=false) => store.Query<Student>(false,historical && caller.IsPrivileged)
        .AnyAsync(s => s.Id == id && (caller.IsPrivileged || s.ServantId == caller.UserId), ct);
    public Task<bool> Eligible(Guid id, CancellationToken ct = default) =>
        (from u in store.Query<ApplicationUser>(false)
         join ur in store.Query<IdentityUserRole<Guid>>(false) on u.Id equals ur.UserId
         join r in store.Query<IdentityRole<Guid>>(false) on ur.RoleId equals r.Id
         where u.Id == id && u.IsActive && (r.Name == "Servant" || r.Name == "Secretary" || r.Name == "Manager")
         select u.Id).AnyAsync(ct);
}
