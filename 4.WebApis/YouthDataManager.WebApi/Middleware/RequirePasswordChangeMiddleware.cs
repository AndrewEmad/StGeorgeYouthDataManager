using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace YouthDataManager.WebApi.Middleware;

public class RequirePasswordChangeMiddleware
{
    private readonly RequestDelegate _next;

    public RequirePasswordChangeMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.User?.Identity?.IsAuthenticated == true)
        {
            var mustChange = context.User.FindFirst("must_change_password")?.Value == "true";
            var path = context.Request.Path.Value ?? "";
            if (mustChange && !path.Contains("Auth/change-password", System.StringComparison.OrdinalIgnoreCase))
            {
                context.Response.StatusCode = 403;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsJsonAsync(new { message = "Password change required.", requiresPasswordChange = true });
                return;
            }
        }
        await _next(context);
    }
}
