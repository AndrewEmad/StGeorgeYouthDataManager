using YouthDataManager.Domain.Entities;

namespace YouthDataManager.Domain.Repositories.Tracking;

public interface IStudentAdditionRequestRepository
{
    Task<StudentAdditionRequest?> GetById(Guid id);
    Task<IEnumerable<StudentAdditionRequest>> GetPending();
    Task<IEnumerable<StudentAdditionRequest>> GetByRequestedByUser(Guid userId);
    void Add(StudentAdditionRequest request);
    void Update(StudentAdditionRequest request);
}
