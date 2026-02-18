using Microsoft.EntityFrameworkCore;
using YouthDataManager.Domain.Entities;
using YouthDataManager.Domain.Enums;
using YouthDataManager.Domain.Repositories.Tracking;
using YouthDataManager.Infrastructure.Data;

namespace YouthDataManager.Infrastructure.Repositories.Tracking;

public class StudentRemovalRequestRepository : IStudentRemovalRequestRepository
{
    private readonly AppDbContext _context;

    public StudentRemovalRequestRepository(AppDbContext context) => _context = context;

    public async Task<StudentRemovalRequest?> GetById(Guid id) =>
        await _context.StudentRemovalRequests
            .Include(r => r.Student)
            .FirstOrDefaultAsync(r => r.Id == id);

    public async Task<StudentRemovalRequest?> GetPendingByStudentAndUser(Guid studentId, Guid requestedByUserId) =>
        await _context.StudentRemovalRequests
            .Include(r => r.Student)
            .Include(r => r.RequestedByUser)
            .FirstOrDefaultAsync(r => r.StudentId == studentId && r.RequestedByUserId == requestedByUserId && r.Status == RemovalRequestStatus.Pending);

    public async Task<IEnumerable<StudentRemovalRequest>> GetPendingRequests() =>
        await _context.StudentRemovalRequests
            .Include(r => r.Student)
            .Include(r => r.RequestedByUser)
            .Where(r => r.Status == RemovalRequestStatus.Pending)
            .OrderBy(r => r.RequestedAt)
            .ToListAsync();

    public async Task<IEnumerable<StudentRemovalRequest>> GetByRequestedByUser(Guid userId) =>
        await _context.StudentRemovalRequests
            .Include(r => r.Student)
            .Include(r => r.RequestedByUser)
            .Where(r => r.RequestedByUserId == userId)
            .OrderByDescending(r => r.RequestedAt)
            .ToListAsync();

    public void Add(StudentRemovalRequest request) => _context.StudentRemovalRequests.Add(request);

    public void Update(StudentRemovalRequest request) => _context.StudentRemovalRequests.Update(request);
}
