using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace YouthDataManager.WebApi.Middleware;

public class RequireProfileCompletionMiddleware
{
    private readonly RequestDelegate _next;

    public RequireProfileCompletionMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.User?.Identity?.IsAuthenticated == true)
        {
            var profileIncomplete = context.User.FindFirst("profile_incomplete")?.Value == "true";
            var path = context.Request.Path.Value ?? "";
            var allowedPath = path.Contains("Profile", System.StringComparison.OrdinalIgnoreCase) ||
                              path.Contains("Auth/change-password", System.StringComparison.OrdinalIgnoreCase);
            if (profileIncomplete && !allowedPath)
            {
                context.Response.StatusCode = 403;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsJsonAsync(new { message = "Profile completion required.", requiresProfileCompletion = true });
                return;
            }
        }
        await _next(context);
    }
}
