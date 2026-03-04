using Microsoft.EntityFrameworkCore;
using YouthDataManager.Domain.Entities;
using YouthDataManager.Domain.Repositories.Tracking;
using YouthDataManager.Infrastructure.Data;

namespace YouthDataManager.Infrastructure.Repositories.Tracking;

public class HomeVisitRepository : IHomeVisitRepository
{
    private readonly AppDbContext _context;

    public HomeVisitRepository(AppDbContext context) => _context = context;

    public async Task<HomeVisit?> GetById(Guid id) =>
        await _context.HomeVisits.FindAsync(id);

    public async Task<HomeVisit?> GetByIdWithParticipants(Guid id) =>
        await _context.HomeVisits.Include(v => v.Participants).FirstOrDefaultAsync(v => v.Id == id);

    public void Add(HomeVisit homeVisit) => _context.HomeVisits.Add(homeVisit);

    public void Update(HomeVisit homeVisit) => _context.HomeVisits.Update(homeVisit);

    public void Remove(HomeVisit homeVisit) => _context.HomeVisits.Remove(homeVisit);
}
