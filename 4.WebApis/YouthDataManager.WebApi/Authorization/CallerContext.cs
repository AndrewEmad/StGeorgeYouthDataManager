using System.Security.Claims;
using YouthDataManager.Shared.Service.Abstractions;

namespace YouthDataManager.WebApi.Authorization;

public sealed class CallerContext(IHttpContextAccessor accessor) : ICallerContext
{
    public bool AllServiceYears => accessor.HttpContext?.Items["AllServiceYears"] is true;
    public ServiceYear? SelectedServiceYear => accessor.HttpContext?.Items["ServiceYear"] as ServiceYear;
    public string? ExpectedVersion => accessor.HttpContext?.Request.Headers["If-Match"].FirstOrDefault()?.Trim('"');
    public DateTime UtcNow { get; } = DateTime.UtcNow;
    public CancellationToken CancellationToken => accessor.HttpContext?.RequestAborted ?? default;
    public Guid? UserId => Guid.TryParse(accessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier),out var id) ? id : null;
    public bool IsPrivileged => accessor.HttpContext?.User.IsAdminOrManagerOrPriest() == true &&
        !PersonalWorkspaceAttribute.IsRequested(accessor.HttpContext);
    public string CorrelationId => accessor.HttpContext?.TraceIdentifier ?? "background";
    public string? Reason
    {
        get
        {
            var headers=accessor.HttpContext?.Request.Headers;var reason=headers?["X-Audit-Reason"].FirstOrDefault();
            return reason!=null && headers?["X-Audit-Reason-Encoding"].FirstOrDefault()=="uri" ? Uri.UnescapeDataString(reason) : reason;
        }
    }
}
