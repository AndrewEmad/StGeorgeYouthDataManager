using YouthDataManager.WebApi.Authorization;
using YouthDataManager.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
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
            var restricted = (context.Items["Account"] as ApplicationUser)?.MustChangePassword == true;
            var allowed = context.GetEndpoint()?.Metadata.GetMetadata<AllowRestrictedAccountAttribute>() != null || context.GetEndpoint()?.Metadata.GetMetadata<IAllowAnonymous>() != null;
            if (restricted && !allowed)
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
