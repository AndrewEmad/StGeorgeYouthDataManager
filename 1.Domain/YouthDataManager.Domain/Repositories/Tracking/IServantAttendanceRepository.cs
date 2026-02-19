using YouthDataManager.Domain.Entities;

namespace YouthDataManager.Domain.Repositories.Tracking;

public interface IServantAttendanceRepository
{
    void Add(ServantAttendance entity);
    void AddRange(IEnumerable<ServantAttendance> entities);
}
