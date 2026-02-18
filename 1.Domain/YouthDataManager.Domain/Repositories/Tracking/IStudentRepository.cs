using YouthDataManager.Domain.Entities;

namespace YouthDataManager.Domain.Repositories.Tracking;

public interface IStudentRepository
{
    Task<Student?> GetById(Guid id);
    Task<Student?> GetByIdWithRelations(Guid id);
    void Add(Student student);
    void Update(Student student);
    void Remove(Student student);
}
