using System.Collections.Generic;
using YouthDataManager.Domain.Entities;

namespace YouthDataManager.Auth.Service.Abstractions;

public interface IJwtTokenService
{
    string GenerateToken(ApplicationUser user, IEnumerable<string> roles, bool mustChangePassword = false);
    string GenerateRefreshToken();
}
