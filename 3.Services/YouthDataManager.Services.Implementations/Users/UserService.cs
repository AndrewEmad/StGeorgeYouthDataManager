using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using YouthDataManager.Domain.Entities;
using YouthDataManager.Domain.Utilities;
using YouthDataManager.Shared.Service.Abstractions;
using YouthDataManager.Users.Service.Abstractions;
using YouthDataManager.Users.Service.Abstractions.DTOs;

namespace YouthDataManager.Users.Service.Implementations;

public class UserService : IUserService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole<Guid>> _roleManager;
    private readonly IActivityLogger _logger;
    private readonly IApplicationStore _store;
    private readonly ICallerContext? _caller;
    private CancellationToken Ct=>_caller?.CancellationToken ?? default;

    public UserService(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole<Guid>> roleManager,
        IActivityLogger logger, IApplicationStore store, ICallerContext? caller=null)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _logger = logger;
        _store = store;_caller=caller;
    }

    public Task<ServiceResult<Guid>> CreateAsync(CreateUserDto dto) => _store.Transaction(async () => { await _store.LockAdministration(); return await CreateAsyncCore(dto); }, r => r.Status == ServiceResultStatus.Success);
    public Task<ServiceResult> UpdateAsync(Guid id, UpdateUserDto dto) => _store.Transaction(async () => { await _store.LockAdministration(); return await UpdateAsyncCore(id, dto); }, r => r.Status == ServiceResultStatus.Success);
    public Task<ServiceResult> UpdateMyProfileAsync(Guid id, UpdateMyProfileDto dto) => _store.Transaction(() => UpdateMyProfileAsyncCore(id, dto), r => r.Status == ServiceResultStatus.Success);
    public Task<ServiceResult> ToggleStatusAsync(Guid id) => _store.Transaction(async () => { await _store.LockAdministration(); return await ToggleStatusAsyncCore(id); }, r => r.Status == ServiceResultStatus.Success);
    public Task<ServiceResult> DeleteAsync(Guid id) => _store.Transaction(async () => { await _store.LockAdministration(); return await DeleteAsyncCore(id); }, r => r.Status == ServiceResultStatus.Success);
    public Task<ServiceResult> SetPasswordAsync(Guid id, SetPasswordDto dto) => _store.Transaction(async () => { await _store.LockAdministration(); return await SetPasswordAsyncCore(id, dto); }, r => r.Status == ServiceResultStatus.Success);

    public async Task<ServiceResult<IEnumerable<UserDto>>> GetAllAsync()
    {
        var users = await _userManager.Users.ToListAsync(Ct);
        var roles=await (from ur in _store.Query<IdentityUserRole<Guid>>(false) join r in _store.Query<IdentityRole<Guid>>(false) on ur.RoleId equals r.Id select new {ur.UserId,r.Name}).ToListAsync(Ct);
        var roleMap=roles.GroupBy(r=>r.UserId).ToDictionary(g=>g.Key,g=>g.OrderBy(r=>r.Name).First().Name ?? "Unknown");
        var dtos=users.Select(user=>new UserDto(user.Id,user.UserName!,user.FullName,user.Email!,user.Phone,roleMap.GetValueOrDefault(user.Id,"Unknown"),user.IsActive,user.CreatedAt,user.UpdatedAt,user.PhotoPath,user.Address)).ToList();
        return ServiceResult<IEnumerable<UserDto>>.Success(dtos);
    }

    public async Task<ServiceResult<PagedResult<UserDto>>> GetPagedAsync(int page, int pageSize, string? search = null, string? role = null, bool? isActive = null, string? sortBy = null, bool? sortDesc = null)
    {
        if (page < 1 || pageSize is < 1 or > 100 || (long)(page - 1) * pageSize > int.MaxValue) return ServiceResult<PagedResult<UserDto>>.Failure("Invalid page or page size (1-100).");

        var membership=from ur in _store.Query<IdentityUserRole<Guid>>(false) join r in _store.Query<IdentityRole<Guid>>(false) on ur.RoleId equals r.Id select new {ur.UserId,r.Name};
        var query=_userManager.Users.Where(u=>!membership.Any(m=>m.UserId==u.Id && m.Name=="Admin"));
        if(role!="Priest")query=query.Where(u=>!membership.Any(m=>m.UserId==u.Id && m.Name=="Priest"));
        if(!string.IsNullOrWhiteSpace(role)) {
            if(!AppRoles.Contains(role))return ServiceResult<PagedResult<UserDto>>.Failure("Invalid role filter.");
            query=query.Where(u=>membership.Any(m=>m.UserId==u.Id && m.Name==role));
        }
        var searchTrim = search?.Trim();
        if (!string.IsNullOrEmpty(searchTrim))
        {
            var normalized = ArabicNormalizer.Normalize(searchTrim);
            query = query.Where(u =>
                (u.NormalizedFullName != null && u.NormalizedFullName.Contains(normalized)) ||
                (u.UserName != null && u.UserName.Contains(searchTrim)) ||
                (u.Phone != null && u.Phone.Contains(searchTrim)));
        }
        if (isActive.HasValue)
            query = query.Where(u => u.IsActive == isActive.Value);

        var totalCount = await query.CountAsync(Ct);
        var sortKey = (sortBy ?? "fullname").Trim().ToLowerInvariant();
        if(sortKey is not ("fullname" or "username" or "isactive"))return ServiceResult<PagedResult<UserDto>>.Failure("Invalid sort key.");
        var desc = sortDesc ?? false;
        var users = sortKey == "username"
            ? await (desc ? query.OrderByDescending(u => u.UserName).ThenBy(u => u.FullName) : query.OrderBy(u => u.UserName).ThenBy(u => u.FullName)).ThenBy(u => u.Id).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(Ct)
            : sortKey == "isactive"
            ? await (desc ? query.OrderByDescending(u => u.IsActive).ThenBy(u => u.FullName) : query.OrderBy(u => u.IsActive).ThenBy(u => u.FullName)).ThenBy(u => u.Id).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(Ct)
            : await (desc ? query.OrderByDescending(u => u.FullName) : query.OrderBy(u => u.FullName)).ThenBy(u => u.Id).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(Ct);
        var ids=users.Select(u=>u.Id).ToArray();
        var roles=await membership.Where(m=>ids.Contains(m.UserId)).ToListAsync(Ct);
        var roleMap=roles.GroupBy(r=>r.UserId).ToDictionary(g=>g.Key,g=>g.OrderBy(r=>r.Name).First().Name ?? "Unknown");
        var dtos=users.Select(user=>new UserDto(user.Id,user.UserName!,user.FullName,user.Email!,user.Phone,roleMap.GetValueOrDefault(user.Id,"Unknown"),user.IsActive,user.CreatedAt,user.UpdatedAt,user.PhotoPath,user.Address)).ToList();
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
            user.UpdatedAt,
            user.PhotoPath,
            user.Address
        );

        return ServiceResult<UserDto>.Success(dto);
    }

    private async Task<ServiceResult<Guid>> CreateAsyncCore(CreateUserDto dto)
    {
        var invalid=ValidateUser(dto.FullName,dto.Phone,null,dto.Role);
        if(invalid!=null || (!string.IsNullOrWhiteSpace(dto.Email) && (dto.Email.Trim().Length>256 || !new System.ComponentModel.DataAnnotations.EmailAddressAttribute().IsValid(dto.Email.Trim()))) || string.IsNullOrWhiteSpace(dto.UserName) || dto.UserName.Trim().Length>100)return ServiceResult<Guid>.Failure(invalid ?? "A valid username is required (up to 100 characters).");

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = dto.UserName.Trim(),
            Email = dto.Email?.Trim(),
            FullName = dto.FullName.Trim(),
            NormalizedFullName = ArabicNormalizer.Normalize(dto.FullName),
            Phone = dto.Phone.Trim(),
            IsActive = true,
            MustChangePassword = true,
            CreatedAt = DateTime.UtcNow
        };

        if (!await _roleManager.RoleExistsAsync(dto.Role))
            return ServiceResult<Guid>.Failure($"الدور «{dto.Role}» غير موجود في النظام.");

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

        var addRoleResult = await _userManager.AddToRoleAsync(user, dto.Role);
        if (!addRoleResult.Succeeded)
        {

            return ServiceResult<Guid>.Failure(addRoleResult.Errors?.FirstOrDefault()?.Description ?? "فشل تعيين الدور.");
        }
        
        await _logger.LogAsync("إضافة مستخدم", $"تم إضافة مستخدم جديد: {user.FullName} ({dto.Role})");
        
        return ServiceResult<Guid>.Success(user.Id);
    }

    private static readonly string[] AppRoles = { "Admin", "Servant", "Manager", "Secretary", "Priest" };

    private static string? ValidateUser(string? name,string? phone,string? address,string? role) => string.IsNullOrWhiteSpace(name) || name.Trim().Length>200 || string.IsNullOrWhiteSpace(phone) || phone.Trim().Length>20 || PhoneFields.Canonical(phone)=="" || (address?.Trim().Length ?? 0)>500 || (role!=null && !AppRoles.Contains(role)) ? "Invalid account fields or role." : null;
    private async Task<ServiceResult> UpdateAsyncCore(Guid id, UpdateUserDto dto)
    {
        var invalid=ValidateUser(dto.FullName,dto.Phone,dto.Address,dto.Role);if(invalid!=null)return ServiceResult.Failure(invalid);

        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user == null) return ServiceResult.Failure("User not found");

        var current = await _userManager.GetRolesAsync(user);
        if (user.IsActive && current.Contains("Admin") && (!dto.IsActive || (!string.IsNullOrWhiteSpace(dto.Role) && dto.Role != "Admin")) &&
            (await _userManager.GetUsersInRoleAsync("Admin")).Count(u => u.IsActive) <= 1)
            return ServiceResult.Failure("The last active Admin must remain active with the Admin role.");

        user.FullName = dto.FullName.Trim();
        user.NormalizedFullName = ArabicNormalizer.Normalize(dto.FullName);
        user.Phone = dto.Phone.Trim();
        user.IsActive = dto.IsActive;
        if (dto.Address != null) user.Address = dto.Address.Trim();
        user.UpdatedAt = DateTime.UtcNow;

        if (!string.IsNullOrWhiteSpace(dto.Role))
        {
            if (!await _roleManager.RoleExistsAsync(dto.Role))
                return ServiceResult.Failure($"الدور «{dto.Role}» غير موجود في النظام.");

            if (dto.Role == "Priest")
            {
                var existingPriests = await _userManager.GetUsersInRoleAsync("Priest");
                if (existingPriests.Count > 0 && existingPriests.All(p => p.Id != id))
                    return ServiceResult.Failure("يسمح بوجود مستخدم واحد فقط بدور الاب الكاهن المسئول.");
            }

            var currentRoles = await _userManager.GetRolesAsync(user);
            foreach (var role in AppRoles)
            {
                if (currentRoles.Contains(role))
                    {
                    var removed = await _userManager.RemoveFromRoleAsync(user, role);
                    if (!removed.Succeeded) return ServiceResult.Failure(string.Join("; ",removed.Errors.Select(e => e.Description)));
                }
            }
            var addRoleResult = await _userManager.AddToRoleAsync(user, dto.Role);
            if (!addRoleResult.Succeeded)
                return ServiceResult.Failure(addRoleResult.Errors?.FirstOrDefault()?.Description ?? "فشل تعيين الدور.");
        }

        user.SecurityStamp = Guid.NewGuid().ToString("N");
        if (!user.IsActive || !(await EligibleUsers.Query(_store).AnyAsync(u => u.Id == user.Id)))
        {
            foreach (var task in await _store.Query<FollowUpTask>().Where(t => t.AssigneeId == user.Id && (t.Status == "Open" || t.Status == "PendingReview")).ToListAsync(Ct)) task.AssigneeId = null;
        }
        var result = await _userManager.UpdateAsync(user);
        
        if (result.Succeeded)
        {
            await _logger.LogAsync("تحديث مستخدم", $"تم تحديث بيانات المستخدم: {user.FullName}");
            return ServiceResult.Success();
        }
        
        return ServiceResult.Failure("Update failed");
    }

    private async Task<ServiceResult> UpdateMyProfileAsyncCore(Guid id, UpdateMyProfileDto dto)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user == null) return ServiceResult.Failure("User not found");

        var roles = await _userManager.GetRolesAsync(user);
        var requiresAddress=!roles.Contains("Admin") && !roles.Contains("Priest");
        if ((dto.FullName != null && (string.IsNullOrWhiteSpace(dto.FullName) || dto.FullName.Trim().Length > 200)) ||
            (dto.Phone != null && (string.IsNullOrWhiteSpace(dto.Phone) || dto.Phone.Trim().Length > 20 || PhoneFields.Canonical(dto.Phone)=="")) ||
            (dto.Address != null && ((requiresAddress && string.IsNullOrWhiteSpace(dto.Address)) || dto.Address.Trim().Length > 500)) || (dto.Email != null && dto.Email.Trim().Length>0 && (dto.Email.Trim().Length>256 || !new System.ComponentModel.DataAnnotations.EmailAddressAttribute().IsValid(dto.Email.Trim()))))
            return ServiceResult.Failure("Invalid required profile fields.");
        if (dto.FullName != null)
        {
            user.FullName = dto.FullName.Trim();
            user.NormalizedFullName = ArabicNormalizer.Normalize(dto.FullName);
        }
        if (dto.Email != null) user.Email = string.IsNullOrWhiteSpace(dto.Email) ? null : dto.Email.Trim();
        if (dto.Phone != null) user.Phone = dto.Phone.Trim();
        if (dto.Address != null) user.Address = dto.Address.Trim();
        user.UpdatedAt = DateTime.UtcNow;

        var result = await _userManager.UpdateAsync(user);
        if (result.Succeeded)
        {
            await _logger.LogAsync("تحديث الملف الشخصي", $"تم تحديث الملف الشخصي: {user.FullName}");
            return ServiceResult.Success();
        }
        return ServiceResult.Failure(result.Errors?.FirstOrDefault()?.Description ?? "Update failed");
    }

    private async Task<ServiceResult> ToggleStatusAsyncCore(Guid id)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user == null) return ServiceResult.Failure("User not found");

        if (user.IsActive && await _userManager.IsInRoleAsync(user,"Admin") && (await _userManager.GetUsersInRoleAsync("Admin")).Count(u => u.IsActive) <= 1)
            return ServiceResult.Failure("The last active Admin cannot be deactivated.");
        user.IsActive = !user.IsActive;
        user.UpdatedAt = DateTime.UtcNow;

        user.SecurityStamp = Guid.NewGuid().ToString("N");
        if (!user.IsActive || !(await EligibleUsers.Query(_store).AnyAsync(u => u.Id == user.Id)))
        {
            foreach (var task in await _store.Query<FollowUpTask>().Where(t => t.AssigneeId == user.Id && (t.Status == "Open" || t.Status == "PendingReview")).ToListAsync(Ct)) task.AssigneeId = null;
        }
        var result = await _userManager.UpdateAsync(user);
        
        if (result.Succeeded)
        {
            var action = user.IsActive ? "تنشيط مستخدم" : "تعطيل مستخدم";
            await _logger.LogAsync(action, $"تم تغيير حالة المستخدم: {user.FullName}");
            return ServiceResult.Success();
        }
        
        return ServiceResult.Failure("Toggle status failed");
    }

    private async Task<ServiceResult> DeleteAsyncCore(Guid id)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user == null) return ServiceResult.Failure("User not found");
        if (!user.IsActive) return ServiceResult.Success();
        return await ToggleStatusAsyncCore(id);
    }

    private async Task<ServiceResult> SetPasswordAsyncCore(Guid id, SetPasswordDto dto)
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
        var updated = await _userManager.UpdateAsync(user);
        if (!updated.Succeeded) return ServiceResult.Failure(string.Join("; ",updated.Errors.Select(e => e.Description)));
        await _logger.LogAsync("تغيير كلمة المرور", $"تم تعيين كلمة مرور جديدة للمستخدم: {user.FullName} (يُطلب منه تغييرها عند أول دخول)");
        return ServiceResult.Success();
    }
}
