using YouthDataManager.Domain.Entities;

namespace YouthDataManager.Domain.Repositories.Tracking;

public interface IStudentRemovalRequestRepository
{
    Task<StudentRemovalRequest?> GetById(Guid id);
    Task<StudentRemovalRequest?> GetPendingByStudentAndUser(Guid studentId, Guid requestedByUserId);
    Task<IEnumerable<StudentRemovalRequest>> GetPendingRequests();
    Task<IEnumerable<StudentRemovalRequest>> GetByRequestedByUser(Guid userId);
    void Add(StudentRemovalRequest request);
    void Update(StudentRemovalRequest request);
}
