using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace YouthDataManager.WebApi.Authorization;

// Opt-in restriction for the servant client. This never grants a role or permission.
// Attendance recording keeps its separate, existing Secretary/Manager authorization.
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class PersonalWorkspaceAttribute : Attribute, IActionFilter
{
    public static bool IsRequested(HttpContext? context) =>
        context?.GetEndpoint()?.Metadata.GetMetadata<PersonalWorkspaceAttribute>() != null &&
        string.Equals(context.Request.Headers["X-YDM-Workspace"].FirstOrDefault(), "personal", StringComparison.Ordinal);

    public void OnActionExecuting(ActionExecutingContext context)
    {
        if (!IsRequested(context.HttpContext)) return;
        // Existing management-only endpoints are used from the admin workspace.
        // Personal endpoints continue to enforce their resource policies normally.
        if (context.HttpContext.GetEndpoint()?.Metadata.GetOrderedMetadata<IAuthorizeData>()
            .Any(a => !string.IsNullOrWhiteSpace(a.Roles) && a.Roles.Split(',')
                .All(r => r.Trim() is "Admin" or "Manager" or "Priest")) == true)
            context.Result = new ForbidResult();
    }

    public void OnActionExecuted(ActionExecutedContext context) { }
}
