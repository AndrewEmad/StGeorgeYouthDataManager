using System;
using System.Collections.Generic;

namespace YouthDataManager.Users.Service.Abstractions.DTOs;

public record PagedResult<T>(IReadOnlyList<T> Items, int TotalCount, int Page, int PageSize);

public record UserDto(
    Guid Id,
    string UserName,
    string FullName,
    string Email,
    string Phone,
    string Role,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);

public record CreateUserDto(
    string UserName,
    string FullName,
    string Email,
    string Phone,
    string Password,
    string Role
);

public record UpdateUserDto(
    string FullName,
    string Phone,
    bool IsActive
);

public record SetPasswordDto(string NewPassword);

public record UpdateMyProfileDto(string? FullName, string? Email, string? Phone);
