using System.Security.Claims;
using YouthDataManager.Shared.Service.Abstractions;

namespace YouthDataManager.WebApi.Authorization;

public sealed class CallerContext(IHttpContextAccessor accessor) : ICallerContext
{
    public string? ExpectedVersion => accessor.HttpContext?.Request.Headers["If-Match"].FirstOrDefault()?.Trim('"');
    public DateTime UtcNow { get; } = DateTime.UtcNow;
    public CancellationToken CancellationToken => accessor.HttpContext?.RequestAborted ?? default;
    public Guid? UserId => Guid.TryParse(accessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier),out var id) ? id : null;
    public bool IsPrivileged => accessor.HttpContext?.User.IsAdminOrManagerOrPriest() == true;
    public string CorrelationId => accessor.HttpContext?.TraceIdentifier ?? "background";
    public string? Reason => accessor.HttpContext?.Request.Headers["X-Audit-Reason"].FirstOrDefault();
}
