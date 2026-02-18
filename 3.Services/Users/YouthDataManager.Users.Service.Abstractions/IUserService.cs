using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using YouthDataManager.Shared.Service.Abstractions;
using YouthDataManager.Users.Service.Abstractions.DTOs;

namespace YouthDataManager.Users.Service.Abstractions;

public interface IUserService
{
    Task<ServiceResult<IEnumerable<UserDto>>> GetAllAsync();
    Task<ServiceResult<PagedResult<UserDto>>> GetPagedAsync(int page, int pageSize, string? search = null, string? role = null, bool? isActive = null);
    Task<ServiceResult<UserDto>> GetByIdAsync(Guid id);
    Task<ServiceResult<Guid>> CreateAsync(CreateUserDto dto);
    Task<ServiceResult> UpdateAsync(Guid id, UpdateUserDto dto);
    Task<ServiceResult> ToggleStatusAsync(Guid id);
    Task<ServiceResult> DeleteAsync(Guid id);
    Task<ServiceResult> SetPasswordAsync(Guid id, SetPasswordDto dto);
}
