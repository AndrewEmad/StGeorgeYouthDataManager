using Microsoft.EntityFrameworkCore;
using YouthDataManager.Domain.Entities;
using YouthDataManager.Domain.Enums;
using YouthDataManager.Domain.Repositories.Tracking;
using YouthDataManager.Infrastructure.Data;

namespace YouthDataManager.Infrastructure.Repositories.Tracking;

public class StudentAssignmentRequestRepository : IStudentAssignmentRequestRepository
{
    private readonly AppDbContext _context;

    public StudentAssignmentRequestRepository(AppDbContext context) => _context = context;

    public async Task<StudentAssignmentRequest?> GetById(Guid id) =>
        await _context.StudentAssignmentRequests
            .Include(r => r.Student)
            .Include(r => r.RequestedByUser)
            .FirstOrDefaultAsync(r => r.Id == id);

    public async Task<StudentAssignmentRequest?> GetPendingByStudent(Guid studentId) =>
        await _context.StudentAssignmentRequests
            .Include(r => r.Student)
            .Include(r => r.RequestedByUser)
            .FirstOrDefaultAsync(r => r.StudentId == studentId && r.Status == AssignmentRequestStatus.Pending);

    public async Task<IEnumerable<StudentAssignmentRequest>> GetPendingRequests() =>
        await _context.StudentAssignmentRequests
            .Include(r => r.Student)
            .Include(r => r.RequestedByUser)
            .Where(r => r.Status == AssignmentRequestStatus.Pending)
            .OrderBy(r => r.RequestedAt)
            .ToListAsync();

    public async Task<IEnumerable<StudentAssignmentRequest>> GetByRequestedByUser(Guid userId) =>
        await _context.StudentAssignmentRequests
            .Include(r => r.Student)
            .Include(r => r.RequestedByUser)
            .Where(r => r.RequestedByUserId == userId)
            .OrderByDescending(r => r.RequestedAt)
            .ToListAsync();

    public async Task<IEnumerable<Guid>> GetStudentIdsWithPendingRequestByOtherThan(Guid servantId) =>
        await _context.StudentAssignmentRequests
            .Where(r => r.Status == AssignmentRequestStatus.Pending && r.RequestedByUserId != servantId)
            .Select(r => r.StudentId)
            .Distinct()
            .ToListAsync();

    public async Task<IEnumerable<Guid>> GetStudentIdsWithPendingRequestByUser(Guid userId) =>
        await _context.StudentAssignmentRequests
            .Where(r => r.Status == AssignmentRequestStatus.Pending && r.RequestedByUserId == userId)
            .Select(r => r.StudentId)
            .ToListAsync();

    public async Task<bool> HasPendingRequestByUser(Guid studentId, Guid userId) =>
        await _context.StudentAssignmentRequests
            .AnyAsync(r => r.StudentId == studentId && r.RequestedByUserId == userId && r.Status == AssignmentRequestStatus.Pending);

    public void Add(StudentAssignmentRequest request) => _context.StudentAssignmentRequests.Add(request);

    public void Update(StudentAssignmentRequest request) => _context.StudentAssignmentRequests.Update(request);
}
