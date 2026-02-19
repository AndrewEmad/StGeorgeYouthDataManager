using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace YouthDataManager.Domain.Repositories.NoTracking;

public interface IStudentNoTrackingRepository
{
    Task<T?> GetById<T>(Guid id, System.Linq.Expressions.Expression<Func<YouthDataManager.Domain.Entities.Student, T>> selector);
    Task<IEnumerable<T>> GetAll<T>(System.Linq.Expressions.Expression<Func<YouthDataManager.Domain.Entities.Student, T>> selector);
    Task<IEnumerable<T>> GetByServantId<T>(Guid servantId, System.Linq.Expressions.Expression<Func<YouthDataManager.Domain.Entities.Student, T>> selector);
    Task<(IEnumerable<T> Items, int TotalCount)> GetPaged<T>(string? search, string? area, string? academicYear, int? gender, Guid? servantId, bool? hasServant, string? sortBy, bool sortDesc, int page, int pageSize, System.Linq.Expressions.Expression<Func<YouthDataManager.Domain.Entities.Student, T>> selector, IEnumerable<Guid>? excludeStudentIds = null);
    Task<IEnumerable<string>> GetDistinctAreasAsync();
    Task<IEnumerable<string>> GetDistinctAcademicYearsAsync();
}
