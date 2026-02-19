using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using YouthDataManager.Domain.Entities;
using YouthDataManager.Shared.Service.Abstractions;
using YouthDataManager.Users.Service.Abstractions;
using YouthDataManager.Users.Service.Abstractions.DTOs;

namespace YouthDataManager.Users.Service.Implementations;

public class UserService : IUserService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole<Guid>> _roleManager;
    private readonly IActivityLogger _logger;

    public UserService(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole<Guid>> roleManager,
        IActivityLogger logger)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _logger = logger;
    }

    public async Task<ServiceResult<IEnumerable<UserDto>>> GetAllAsync()
    {
        var users = await _userManager.Users.ToListAsync();
        var dtos = new List<UserDto>();

        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            dtos.Add(new UserDto(
                user.Id,
                user.UserName!,
                user.FullName,
                user.Email!,
                user.Phone,
                roles.FirstOrDefault() ?? "Unknown",
                user.IsActive,
                user.CreatedAt,
                user.UpdatedAt
            ));
        }

        return ServiceResult<IEnumerable<UserDto>>.Success(dtos);
    }

    public async Task<ServiceResult<PagedResult<UserDto>>> GetPagedAsync(int page, int pageSize, string? search = null, string? role = null, bool? isActive = null)
    {
        if (page < 1) page = 1;
        if (pageSize > 50) pageSize = 50;
        if (pageSize < 1) pageSize = 10;

        var adminUsers = await _userManager.GetUsersInRoleAsync("Admin");
        var adminIds = adminUsers.Select(u => u.Id).ToList();
        var query = _userManager.Users.AsQueryable().Where(u => !adminIds.Contains(u.Id));

        if (role != "Priest")
        {
            var priestUsers = await _userManager.GetUsersInRoleAsync("Priest");
            var priestIds = priestUsers.Select(u => u.Id).ToList();
            query = query.Where(u => !priestIds.Contains(u.Id));
        }

        if (!string.IsNullOrWhiteSpace(role))
        {
            var roleUsers = await _userManager.GetUsersInRoleAsync(role);
            var roleIds = roleUsers.Select(u => u.Id).ToList();
            query = query.Where(u => roleIds.Contains(u.Id));
        }
        var searchTrim = search?.Trim();
        if (!string.IsNullOrEmpty(searchTrim))
        {
            var lower = searchTrim.ToLower();
            query = query.Where(u =>
                (u.FullName != null && u.FullName.ToLower().Contains(lower)) ||
                (u.UserName != null && u.UserName.ToLower().Contains(lower)) ||
                (u.Phone != null && u.Phone.ToLower().Contains(lower)));
        }
        if (isActive.HasValue)
            query = query.Where(u => u.IsActive == isActive.Value);

        var totalCount = await query.CountAsync();
        var users = await query.OrderBy(u => u.FullName).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        var dtos = new List<UserDto>();
        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            dtos.Add(new UserDto(user.Id, user.UserName!, user.FullName, user.Email!, user.Phone, roles.FirstOrDefault() ?? "Unknown", user.IsActive, user.CreatedAt, user.UpdatedAt));
        }
        return ServiceResult<PagedResult<UserDto>>.Success(new PagedResult<UserDto>(dtos, totalCount, page, pageSize));
    }

    public async Task<ServiceResult<UserDto>> GetByIdAsync(Guid id)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user == null) return ServiceResult<UserDto>.Failure("User not found");

        var roles = await _userManager.GetRolesAsync(user);
        var dto = new UserDto(
            user.Id,
            user.UserName!,
            user.FullName,
            user.Email!,
            user.Phone,
            roles.FirstOrDefault() ?? "Unknown",
            user.IsActive,
            user.CreatedAt,
            user.UpdatedAt
        );

        return ServiceResult<UserDto>.Success(dto);
    }

    public async Task<ServiceResult<Guid>> CreateAsync(CreateUserDto dto)
    {
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = dto.UserName,
            Email = dto.Email,
            FullName = dto.FullName,
            Phone = dto.Phone,
            IsActive = true,
            MustChangePassword = true,
            CreatedAt = DateTime.UtcNow
        };

        if (dto.Role == "Priest")
        {
            var existingPriests = await _userManager.GetUsersInRoleAsync("Priest");
            if (existingPriests.Count > 0)
                return ServiceResult<Guid>.Failure("يسمح بوجود مستخدم واحد فقط بدور الاب الكاهن المسئول.");
        }

        var result = await _userManager.CreateAsync(user, dto.Password);
        if (!result.Succeeded)
        {
            return ServiceResult<Guid>.Failure(string.Join(", ", result.Errors.Select(e => e.Description)));
        }

        await _userManager.AddToRoleAsync(user, dto.Role);
        
        await _logger.LogAsync("إضافة مستخدم", $"تم إضافة مستخدم جديد: {user.FullName} ({dto.Role})");
        
        return ServiceResult<Guid>.Success(user.Id);
    }

    public async Task<ServiceResult> UpdateAsync(Guid id, UpdateUserDto dto)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user == null) return ServiceResult.Failure("User not found");

        user.FullName = dto.FullName;
        user.Phone = dto.Phone;
        user.IsActive = dto.IsActive;
        user.UpdatedAt = DateTime.UtcNow;

        var result = await _userManager.UpdateAsync(user);
        
        if (result.Succeeded)
        {
            await _logger.LogAsync("تحديث مستخدم", $"تم تحديث بيانات المستخدم: {user.FullName}");
            return ServiceResult.Success();
        }
        
        return ServiceResult.Failure("Update failed");
    }

    public async Task<ServiceResult> UpdateMyProfileAsync(Guid id, UpdateMyProfileDto dto)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user == null) return ServiceResult.Failure("User not found");

        if (dto.FullName != null) user.FullName = dto.FullName;
        if (dto.Email != null) user.Email = dto.Email;
        if (dto.Phone != null) user.Phone = dto.Phone;
        user.UpdatedAt = DateTime.UtcNow;

        var result = await _userManager.UpdateAsync(user);
        if (result.Succeeded)
        {
            await _logger.LogAsync("تحديث الملف الشخصي", $"تم تحديث الملف الشخصي: {user.FullName}");
            return ServiceResult.Success();
        }
        return ServiceResult.Failure(result.Errors?.FirstOrDefault()?.Description ?? "Update failed");
    }

    public async Task<ServiceResult> ToggleStatusAsync(Guid id)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user == null) return ServiceResult.Failure("User not found");

        user.IsActive = !user.IsActive;
        user.UpdatedAt = DateTime.UtcNow;

        var result = await _userManager.UpdateAsync(user);
        
        if (result.Succeeded)
        {
            var action = user.IsActive ? "تنشيط مستخدم" : "تعطيل مستخدم";
            await _logger.LogAsync(action, $"تم تغيير حالة المستخدم: {user.FullName}");
            return ServiceResult.Success();
        }
        
        return ServiceResult.Failure("Toggle status failed");
    }

    public async Task<ServiceResult> DeleteAsync(Guid id)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user == null) return ServiceResult.Failure("User not found");
        var roles = await _userManager.GetRolesAsync(user);
        if (roles.Contains("Admin")) return ServiceResult.Failure("Cannot delete Admin user");
        var result = await _userManager.DeleteAsync(user);
        if (result.Succeeded)
        {
            await _logger.LogAsync("حذف مستخدم", $"تم حذف المستخدم: {user.FullName}");
            return ServiceResult.Success();
        }
        return ServiceResult.Failure(string.Join(", ", result.Errors.Select(e => e.Description)));
    }

    public async Task<ServiceResult> SetPasswordAsync(Guid id, SetPasswordDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.NewPassword) || dto.NewPassword.Length < 6)
            return ServiceResult.Failure("كلمة المرور يجب أن تكون 6 أحرف على الأقل.");
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user == null) return ServiceResult.Failure("User not found");
        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var result = await _userManager.ResetPasswordAsync(user, token, dto.NewPassword);
        if (!result.Succeeded)
            return ServiceResult.Failure(string.Join(", ", result.Errors.Select(e => e.Description)));
        user.MustChangePassword = true;
        user.UpdatedAt = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);
        await _logger.LogAsync("تغيير كلمة المرور", $"تم تعيين كلمة مرور جديدة للمستخدم: {user.FullName} (يُطلب منه تغييرها عند أول دخول)");
        return ServiceResult.Success();
    }
}
