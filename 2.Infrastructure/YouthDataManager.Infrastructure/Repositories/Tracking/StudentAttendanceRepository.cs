using YouthDataManager.Domain.Entities;
using YouthDataManager.Domain.Repositories.Tracking;
using YouthDataManager.Infrastructure.Data;

namespace YouthDataManager.Infrastructure.Repositories.Tracking;

public class StudentAttendanceRepository : IStudentAttendanceRepository
{
    private readonly AppDbContext _context;

    public StudentAttendanceRepository(AppDbContext context) => _context = context;

    public void Add(StudentAttendance entity) => _context.StudentAttendances.Add(entity);

    public void AddRange(IEnumerable<StudentAttendance> entities) => _context.StudentAttendances.AddRange(entities);
}
