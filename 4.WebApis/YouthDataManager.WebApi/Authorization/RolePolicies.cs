using System.Linq;
using System.Security.Claims;

namespace YouthDataManager.WebApi.Authorization;

/// <summary>
/// Admin, Manager, and Priest have the same authority in the API.
/// </summary>
public static class RolePolicies
{
    public const string AdminManagerPriestRoles = "Admin,Manager,Priest";
    public const string CanRecordStudentAttendance = "Admin,Manager,Priest,Secretary";

    public static bool IsAdminOrManagerOrPriest(this ClaimsPrincipal user) =>
        user.IsInRole("Admin") || user.IsInRole("Manager") || user.IsInRole("Priest");

    public static bool CanRecordStudentAttendanceRole(this ClaimsPrincipal user) =>
        user.IsInRole("Admin") || user.IsInRole("Manager") || user.IsInRole("Priest") || user.IsInRole("Secretary");
}
