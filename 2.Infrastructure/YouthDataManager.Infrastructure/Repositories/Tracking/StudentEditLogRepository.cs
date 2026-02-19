using YouthDataManager.Domain.Entities;
using YouthDataManager.Domain.Repositories.Tracking;
using YouthDataManager.Infrastructure.Data;

namespace YouthDataManager.Infrastructure.Repositories.Tracking;

public class StudentEditLogRepository : IStudentEditLogRepository
{
    private readonly AppDbContext _context;

    public StudentEditLogRepository(AppDbContext context) => _context = context;

    public void Add(StudentEditLog entity) => _context.StudentEditLogs.Add(entity);
}
