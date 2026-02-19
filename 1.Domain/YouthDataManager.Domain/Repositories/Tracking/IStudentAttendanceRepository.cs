using YouthDataManager.Domain.Entities;

namespace YouthDataManager.Domain.Repositories.Tracking;

public interface IStudentAttendanceRepository
{
    void Add(StudentAttendance entity);
    void AddRange(IEnumerable<StudentAttendance> entities);
}
