using YouthDataManager.Domain.Entities;

namespace YouthDataManager.Domain.Repositories.Tracking;

public interface IUserDeviceTokenRepository
{
    Task<List<UserDeviceToken>> GetByUserId(Guid userId);
    Task<List<UserDeviceToken>> GetByUserIdAndAppType(Guid userId, string appType);
    Task<UserDeviceToken?> GetByToken(string token);
    void Add(UserDeviceToken deviceToken);
    void Remove(UserDeviceToken deviceToken);
    void Update(UserDeviceToken deviceToken);
}
