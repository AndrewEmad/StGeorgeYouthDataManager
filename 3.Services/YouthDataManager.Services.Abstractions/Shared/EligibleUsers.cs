using Microsoft.AspNetCore.Identity;

using YouthDataManager.Domain.Entities;

namespace YouthDataManager.Shared.Service.Abstractions;

public static class EligibleUsers
{
    public static IQueryable<ApplicationUser> Query(IApplicationStore store)
    {
        var memberships = store.Query<IdentityUserRole<Guid>>(false);
        var roles = store.Query<IdentityRole<Guid>>(false);
        return from u in store.Query<ApplicationUser>()
        where u.IsActive && (from ur in memberships
                            join r in roles on ur.RoleId equals r.Id
                            where ur.UserId == u.Id && (r.Name == "Servant" || r.Name == "Secretary" || r.Name == "Manager")
                            select ur.UserId).Any()
        select u;
    }

}
