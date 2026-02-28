using YouthDataManager.Domain.Entities;

namespace YouthDataManager.Domain.Repositories.Tracking;

public interface IUserDeviceTokenRepository
{
    Task<List<UserDeviceToken>> GetByUserId(Guid userId);
    Task<UserDeviceToken?> GetByToken(string token);
    void Add(UserDeviceToken deviceToken);
    void Remove(UserDeviceToken deviceToken);
    void Update(UserDeviceToken deviceToken);
}
