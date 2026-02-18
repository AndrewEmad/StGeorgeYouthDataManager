using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using YouthDataManager.Domain.Entities;
using YouthDataManager.Domain.Repositories.Tracking;
using YouthDataManager.Shared.Service.Abstractions;
using YouthDataManager.Infrastructure.Data;

namespace YouthDataManager.Shared.Service.Implementations
{
    public class ActivityLogger : IActivityLogger
    {
        private readonly AppDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ActivityLogger(AppDbContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task LogAsync(string action, string details)
        {
            var user = _httpContextAccessor.HttpContext?.User;
            var userName = user?.Identity?.Name ?? "System";
            var userRole = user?.FindFirst(ClaimTypes.Role)?.Value ?? "None";

            var log = new ActivityLog(action, details, userName, userRole);

            _context.ActivityLogs.Add(log);
            await _context.SaveChangesAsync();
        }
    }
}
