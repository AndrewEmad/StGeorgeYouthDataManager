using YouthDataManager.Domain.Entities;

namespace YouthDataManager.Domain.Repositories.Tracking;

public interface IStudentEditLogRepository
{
    void Add(StudentEditLog entity);
}
