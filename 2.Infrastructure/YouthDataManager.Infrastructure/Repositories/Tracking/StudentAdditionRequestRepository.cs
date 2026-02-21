using Microsoft.EntityFrameworkCore;
using YouthDataManager.Domain.Entities;
using YouthDataManager.Domain.Enums;
using YouthDataManager.Domain.Repositories.Tracking;
using YouthDataManager.Infrastructure.Data;

namespace YouthDataManager.Infrastructure.Repositories.Tracking;

public class StudentAdditionRequestRepository : IStudentAdditionRequestRepository
{
    private readonly AppDbContext _context;

    public StudentAdditionRequestRepository(AppDbContext context) => _context = context;

    public async Task<StudentAdditionRequest?> GetById(Guid id) =>
        await _context.StudentAdditionRequests
            .Include(r => r.RequestedByUser)
            .FirstOrDefaultAsync(r => r.Id == id);

    public async Task<IEnumerable<StudentAdditionRequest>> GetPending() =>
        await _context.StudentAdditionRequests
            .Include(r => r.RequestedByUser)
            .Where(r => r.Status == AssignmentRequestStatus.Pending)
            .OrderBy(r => r.RequestedAt)
            .ToListAsync();

    public async Task<IEnumerable<StudentAdditionRequest>> GetByRequestedByUser(Guid userId) =>
        await _context.StudentAdditionRequests
            .Include(r => r.RequestedByUser)
            .Where(r => r.RequestedByUserId == userId)
            .OrderByDescending(r => r.RequestedAt)
            .ToListAsync();

    public void Add(StudentAdditionRequest request) => _context.StudentAdditionRequests.Add(request);

    public void Update(StudentAdditionRequest request) => _context.StudentAdditionRequests.Update(request);
}
