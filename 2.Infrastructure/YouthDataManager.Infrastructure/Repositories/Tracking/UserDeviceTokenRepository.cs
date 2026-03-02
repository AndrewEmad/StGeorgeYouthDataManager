using Microsoft.EntityFrameworkCore;
using YouthDataManager.Domain.Entities;
using YouthDataManager.Domain.Repositories.Tracking;
using YouthDataManager.Infrastructure.Data;

namespace YouthDataManager.Infrastructure.Repositories.Tracking;

public class UserDeviceTokenRepository : IUserDeviceTokenRepository
{
    private readonly AppDbContext _context;

    public UserDeviceTokenRepository(AppDbContext context) => _context = context;

    public async Task<List<UserDeviceToken>> GetByUserId(Guid userId) =>
        await _context.UserDeviceTokens
            .Where(t => t.UserId == userId)
            .ToListAsync();

    public async Task<List<UserDeviceToken>> GetByUserIdAndAppType(Guid userId, string appType) =>
        await _context.UserDeviceTokens
            .Where(t => t.UserId == userId && t.AppType == appType)
            .ToListAsync();

    public async Task<UserDeviceToken?> GetByToken(string token) =>
        await _context.UserDeviceTokens
            .FirstOrDefaultAsync(t => t.Token == token);

    public void Add(UserDeviceToken deviceToken) => _context.UserDeviceTokens.Add(deviceToken);

    public void Remove(UserDeviceToken deviceToken) => _context.UserDeviceTokens.Remove(deviceToken);

    public void Update(UserDeviceToken deviceToken) => _context.UserDeviceTokens.Update(deviceToken);
}
