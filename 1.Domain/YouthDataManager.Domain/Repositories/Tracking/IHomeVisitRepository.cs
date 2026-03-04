using YouthDataManager.Domain.Entities;

namespace YouthDataManager.Domain.Repositories.Tracking;

public interface IHomeVisitRepository
{
    Task<HomeVisit?> GetById(Guid id);
    Task<HomeVisit?> GetByIdWithParticipants(Guid id);
    void Add(HomeVisit homeVisit);
    void Update(HomeVisit homeVisit);
    void Remove(HomeVisit homeVisit);
}
