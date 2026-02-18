using YouthDataManager.Domain.Entities;

namespace YouthDataManager.Domain.Repositories.Tracking;

public interface ICallLogRepository
{
    Task<CallLog?> GetById(Guid id);
    void Add(CallLog callLog);
    void Update(CallLog callLog);
    void Remove(CallLog callLog);
}
