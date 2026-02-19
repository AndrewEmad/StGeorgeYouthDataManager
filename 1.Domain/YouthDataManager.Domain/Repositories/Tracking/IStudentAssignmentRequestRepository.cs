using YouthDataManager.Domain.Entities;

namespace YouthDataManager.Domain.Repositories.Tracking;

public interface IStudentAssignmentRequestRepository
{
    Task<StudentAssignmentRequest?> GetById(Guid id);
    Task<StudentAssignmentRequest?> GetPendingByStudent(Guid studentId);
    Task<IEnumerable<StudentAssignmentRequest>> GetPendingRequests();
    Task<IEnumerable<StudentAssignmentRequest>> GetByRequestedByUser(Guid userId);
    Task<IEnumerable<Guid>> GetStudentIdsWithPendingRequestByOtherThan(Guid servantId);
    Task<IEnumerable<Guid>> GetStudentIdsWithPendingRequestByUser(Guid userId);
    Task<bool> HasPendingRequestByUser(Guid studentId, Guid userId);
    void Add(StudentAssignmentRequest request);
    void Update(StudentAssignmentRequest request);
}
