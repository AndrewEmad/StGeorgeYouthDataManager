using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using YouthDataManager.Domain.Entities;
using YouthDataManager.Domain.Repositories.NoTracking;
using YouthDataManager.Infrastructure.Data;

namespace YouthDataManager.Infrastructure.Repositories.NoTracking;

public class StudentEditLogNoTrackingRepository : IStudentEditLogNoTrackingRepository
{
    private readonly AppDbContext _context;

    public StudentEditLogNoTrackingRepository(AppDbContext context) => _context = context;

    public async Task<IEnumerable<T>> GetByStudentId<T>(Guid studentId, Expression<Func<StudentEditLog, T>> selector)
    {
        return await _context.StudentEditLogs
            .AsNoTracking()
            .Where(e => e.StudentId == studentId)
            .OrderByDescending(e => e.UpdatedAt)
            .Select(selector)
            .ToListAsync();
    }
}
