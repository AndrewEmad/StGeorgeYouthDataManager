using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using YouthDataManager.Auth.Service.Abstractions;
using YouthDataManager.Auth.Service.Abstractions.Commands;
using YouthDataManager.Auth.Service.Abstractions.DTOs;
using YouthDataManager.Domain.Entities;
using YouthDataManager.Shared.Service.Abstractions;

namespace YouthDataManager.Auth.Service.Implementations.Commands;

public class AuthCommandsService : IAuthCommandsService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IJwtTokenService _jwtService;
    private readonly ILogger<AuthCommandsService> _logger;

    public AuthCommandsService(
        UserManager<ApplicationUser> userManager,
        IJwtTokenService jwtService,
        ILogger<AuthCommandsService> logger)
    {
        _userManager = userManager;
        _jwtService = jwtService;
        _logger = logger;
    }

    public async Task<ServiceResult<LoginResponse>> Login(LoginRequest request)
    {
        try
        {
            var user = await _userManager.FindByNameAsync(request.Username);
            if (user == null || !await _userManager.CheckPasswordAsync(user, request.Password))
            {
                return ServiceResult<LoginResponse>.Failure("Invalid username or password");
            }

            if (!user.IsActive)
            {
                return ServiceResult<LoginResponse>.Failure("Your account is deactivated. Please contact your administrator.");
            }

            var roles = await _userManager.GetRolesAsync(user);
            var mustChange = user.MustChangePassword;
            var token = _jwtService.GenerateToken(user, roles, mustChange);
            var refreshToken = _jwtService.GenerateRefreshToken();

            return ServiceResult<LoginResponse>.Success(new LoginResponse(
                token,
                refreshToken,
                user.FullName,
                roles.FirstOrDefault() ?? "Servant",
                user.Id,
                RequiresPasswordChange: mustChange
            ));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login for user {Username}", request.Username);
            return ServiceResult<LoginResponse>.Failure("An error occurred during the login process.");
        }
    }

    public async Task<ServiceResult<LoginResponse>> RefreshToken(RefreshTokenRequest request)
    {
        return ServiceResult<LoginResponse>.Failure("Refresh token logic not fully implemented yet.");
    }

    public async Task<ServiceResult<LoginResponse>> ChangePassword(Guid userId, ChangePasswordRequest request)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
                return ServiceResult<LoginResponse>.Failure("User not found.");
            if (!user.MustChangePassword)
                return ServiceResult<LoginResponse>.Failure("Password change is not required.");
            if (string.IsNullOrWhiteSpace(request.NewPassword) || request.NewPassword.Length < 6)
                return ServiceResult<LoginResponse>.Failure("New password must be at least 6 characters.");
            var changeResult = await _userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
            if (!changeResult.Succeeded)
                return ServiceResult<LoginResponse>.Failure(string.Join("; ", changeResult.Errors.Select(e => e.Description)));
            user.MustChangePassword = false;
            user.UpdatedAt = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);
            var roles = await _userManager.GetRolesAsync(user);
            var jwt = _jwtService.GenerateToken(user, roles, false);
            var refreshToken = _jwtService.GenerateRefreshToken();
            return ServiceResult<LoginResponse>.Success(new LoginResponse(jwt, refreshToken, user.FullName, roles.FirstOrDefault() ?? "Servant", user.Id, RequiresPasswordChange: false));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error changing password for user {UserId}", userId);
            return ServiceResult<LoginResponse>.Failure("An error occurred while changing password.");
        }
    }
}
