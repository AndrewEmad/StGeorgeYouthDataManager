using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using YouthDataManager.Auth.Service.Abstractions;
using YouthDataManager.Auth.Service.Abstractions.Commands;
using YouthDataManager.Auth.Service.Abstractions.DTOs;
using YouthDataManager.Domain.Entities;
using YouthDataManager.Shared.Service.Abstractions;

namespace YouthDataManager.Auth.Service.Implementations.Commands;

public sealed class AuthCommandsService(UserManager<ApplicationUser> users, IJwtTokenService jwt, IApplicationStore store) : IAuthCommandsService
{
    private static string Hash(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
    private static ServiceResult<LoginResponse> Invalid() => ServiceResult<LoginResponse>.Failure("Invalid or expired session.");

    public Task<ServiceResult<LoginResponse>> Login(LoginRequest request) => store.Transaction(async () =>
    {
        var user = await users.FindByNameAsync(request.Username?.Trim() ?? "");
        if (user == null || !user.IsActive || await users.IsLockedOutAsync(user)) return Invalid();
        if (!await users.CheckPasswordAsync(user, request.Password))
        {
            var failed=await users.AccessFailedAsync(user);
            if(!failed.Succeeded)return Invalid();
            return Invalid();
        }
        var reset = await users.ResetAccessFailedCountAsync(user);
        if (!reset.Succeeded) return Invalid();
        return await Issue(user, Guid.NewGuid());
    });

    public async Task<ServiceResult<LoginResponse>> Issue(ApplicationUser user, Guid family)
    {
        if (!user.IsActive) return Invalid();
        var roles = await users.GetRolesAsync(user);
        var incomplete = string.IsNullOrWhiteSpace(user.FullName) || user.FullName.Trim().Length>200 || PhoneFields.Canonical(user.Phone)=="" || user.Phone.Trim().Length>20 || ((!roles.Contains("Admin") && !roles.Contains("Priest")) && (string.IsNullOrWhiteSpace(user.Address) || user.Address.Trim().Length>500));
        var refresh = jwt.GenerateRefreshToken();
        store.Add(new RefreshSession { UserId = user.Id, TokenHash = Hash(refresh), FamilyId = family,
            SecurityStamp = user.SecurityStamp ?? "", ExpiresAt = DateTime.UtcNow.AddDays(30) });
        await store.Save();
        return ServiceResult<LoginResponse>.Success(new LoginResponse(jwt.GenerateToken(user, roles, user.MustChangePassword, incomplete, family),
            refresh, user.FullName, roles.FirstOrDefault() ?? "Servant", user.Id, user.MustChangePassword, incomplete));
    }

    public Task<ServiceResult<LoginResponse>> RefreshToken(RefreshTokenRequest request) => store.Transaction(async () =>
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken) || request.RefreshToken.Length > 512) return Invalid();
        var hash = Hash(request.RefreshToken);
        var session = await store.Query<RefreshSession>().SingleOrDefaultAsync(s => s.TokenHash == hash);
        if (session == null) return Invalid();
        var now = DateTime.UtcNow;
        if (session.RevokedAt != null)
        {
            // A replay revokes every descendant. Commit the revocation even though refresh fails.
            foreach (var s in await store.Query<RefreshSession>().Where(s => s.FamilyId == session.FamilyId && s.RevokedAt == null).ToListAsync()) s.RevokedAt = now;
            var compromised=await users.FindByIdAsync(session.UserId.ToString());
            if(compromised!=null){var revoke=await users.UpdateSecurityStampAsync(compromised);if(!revoke.Succeeded)throw new InvalidOperationException("Could not revoke compromised session.");}
            return Invalid();
        }
        session.RevokedAt = now;
        var user = await users.FindByIdAsync(session.UserId.ToString());
        if (user == null || !user.IsActive || session.ExpiresAt <= now || session.SecurityStamp != user.SecurityStamp) return Invalid();
        return await Issue(user, session.FamilyId);
    });

    public Task<ServiceResult> Logout(Guid userId,string? refreshToken,string? deviceToken=null) => store.Transaction(async()=>
    {
        if(string.IsNullOrWhiteSpace(refreshToken) || refreshToken.Length>512)return ServiceResult.Failure("Refresh token required.");
        var hash=Hash(refreshToken);var session=await store.Query<RefreshSession>().SingleOrDefaultAsync(s=>s.TokenHash==hash);
        if(session==null || (userId!=Guid.Empty && session.UserId!=userId))return ServiceResult.Success();
        foreach(var active in await store.Query<RefreshSession>().Where(s=>s.FamilyId==session.FamilyId && s.RevokedAt==null).ToListAsync())active.RevokedAt=DateTime.UtcNow;
        if(!string.IsNullOrEmpty(deviceToken))foreach(var device in await store.Query<UserDeviceToken>().Where(d=>d.UserId==session.UserId && d.Token==deviceToken).ToListAsync())store.Remove(device);
        return ServiceResult.Success();
    },r=>r.Status==ServiceResultStatus.Success);

    public Task<ServiceResult<LoginResponse>> ChangePassword(Guid userId, ChangePasswordRequest request) => store.Transaction(async () =>
    {
        var user = await users.FindByIdAsync(userId.ToString());
        if (user == null || !user.IsActive) return Invalid();
        var change = await users.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
        if (!change.Succeeded) return ServiceResult<LoginResponse>.Failure(string.Join("; ", change.Errors.Select(e => e.Description)));
        user.MustChangePassword = false;
        user.UpdatedAt = DateTime.UtcNow;
        var update = await users.UpdateAsync(user);
        if (!update.Succeeded) return ServiceResult<LoginResponse>.Failure(string.Join("; ", update.Errors.Select(e => e.Description)));
        foreach (var s in await store.Query<RefreshSession>().Where(s => s.UserId == userId && s.RevokedAt == null).ToListAsync()) s.RevokedAt = DateTime.UtcNow;
        return await Issue(user, Guid.NewGuid());
    }, r => r.Status == ServiceResultStatus.Success);
}
