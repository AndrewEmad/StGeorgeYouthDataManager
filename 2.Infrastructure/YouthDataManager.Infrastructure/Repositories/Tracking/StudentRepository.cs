using Microsoft.EntityFrameworkCore;
using YouthDataManager.Domain.Entities;
using YouthDataManager.Domain.Repositories.Tracking;
using YouthDataManager.Infrastructure.Data;

namespace YouthDataManager.Infrastructure.Repositories.Tracking;

public class StudentRepository : IStudentRepository
{
    private readonly AppDbContext _context;

    public StudentRepository(AppDbContext context) => _context = context;

    public async Task<Student?> GetById(Guid id) =>
        await _context.Students.FindAsync(id);

    public async Task<Student?> GetByIdWithRelations(Guid id) =>
        await _context.Students
            .Include(e => e.Servant)
            .Include(e => e.CallLogs)
            .Include(e => e.HomeVisits)
            .FirstOrDefaultAsync(e => e.Id == id);

    public void Add(Student student) => _context.Students.Add(student);

    public void Update(Student student) => _context.Students.Update(student);

    public void Remove(Student student)
    {
        student.IsDeleted = true;
        _context.Students.Update(student);
    }
}
