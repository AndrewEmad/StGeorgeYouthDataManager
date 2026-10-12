using YouthDataManager.WebApi.Authorization;
using YouthDataManager.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
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
            var restricted = context.Items["ProfileIncomplete"] is true;
            var allowed = context.GetEndpoint()?.Metadata.GetMetadata<AllowRestrictedAccountAttribute>() != null || context.GetEndpoint()?.Metadata.GetMetadata<IAllowAnonymous>() != null;
            if (restricted && !allowed)
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
