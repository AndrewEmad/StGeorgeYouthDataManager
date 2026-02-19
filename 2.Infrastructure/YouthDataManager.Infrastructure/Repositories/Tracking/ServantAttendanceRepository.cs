using YouthDataManager.Domain.Entities;
using YouthDataManager.Domain.Repositories.Tracking;
using YouthDataManager.Infrastructure.Data;

namespace YouthDataManager.Infrastructure.Repositories.Tracking;

public class ServantAttendanceRepository : IServantAttendanceRepository
{
    private readonly AppDbContext _context;

    public ServantAttendanceRepository(AppDbContext context) => _context = context;

    public void Add(ServantAttendance entity) => _context.ServantAttendances.Add(entity);

    public void AddRange(IEnumerable<ServantAttendance> entities) => _context.ServantAttendances.AddRange(entities);
}
