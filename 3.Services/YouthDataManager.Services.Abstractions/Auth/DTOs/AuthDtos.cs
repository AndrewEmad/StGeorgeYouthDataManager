using System;
using System.Collections.Generic;

namespace YouthDataManager.Auth.Service.Abstractions.DTOs;

public record LoginRequest(string Username, string Password);

public record LoginResponse(
    string Token,
    string RefreshToken,
    string FullName,
    string Role,
    Guid UserId,
    bool RequiresPasswordChange = false
);

public record ChangePasswordRequest(string CurrentPassword, string NewPassword);

public record RefreshTokenRequest(string Token, string RefreshToken);

public record UserProfileDto(
    Guid Id,
    string FullName,
    string UserName,
    string Role,
    bool IsActive
);
