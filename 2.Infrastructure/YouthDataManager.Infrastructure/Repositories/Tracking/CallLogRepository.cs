using Microsoft.EntityFrameworkCore;
using YouthDataManager.Domain.Entities;
using YouthDataManager.Domain.Repositories.Tracking;
using YouthDataManager.Infrastructure.Data;

namespace YouthDataManager.Infrastructure.Repositories.Tracking;

public class CallLogRepository : ICallLogRepository
{
    private readonly AppDbContext _context;

    public CallLogRepository(AppDbContext context) => _context = context;

    public async Task<CallLog?> GetById(Guid id) =>
        await _context.CallLogs.FindAsync(id);

    public void Add(CallLog callLog) => _context.CallLogs.Add(callLog);

    public void Update(CallLog callLog) => _context.CallLogs.Update(callLog);

    public void Remove(CallLog callLog) => _context.CallLogs.Remove(callLog);
}
