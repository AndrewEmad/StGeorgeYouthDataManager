using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using YouthDataManager.Reports.Service.Abstractions.DTOs;

namespace YouthDataManager.Reports.Service.Abstractions.Data;

public interface IReportDataProvider
{
    Task<(IReadOnlyList<StudentNoContactDto> Items, int TotalCount)> GetStudentsWithNoRecentContactPagedAsync(int days, Guid? servantId, int page, int pageSize);
    Task<(IReadOnlyList<StudentsByGroupDto> Items, int TotalCount)> GetStudentsByAreaPagedAsync(int page, int pageSize);
    Task<(IReadOnlyList<StudentsByGroupDto> Items, int TotalCount)> GetStudentsByAcademicYearPagedAsync(int page, int pageSize);
}
