using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using YouthDataManager.Domain.Entities;
using YouthDataManager.Auth.Service.Abstractions;

namespace YouthDataManager.Auth.Service.Implementations.Services;

public class JwtTokenService : IJwtTokenService
{
    private readonly JwtConfiguration _config;

    public JwtTokenService(IOptions<JwtConfiguration> config)
    {
        _config = config.Value;
    }

    public string GenerateToken(ApplicationUser user, IEnumerable<string> roles, bool mustChangePassword = false)
    {
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config.Secret));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.UserName ?? string.Empty),
            new(ClaimTypes.Email, user.Email ?? string.Empty),
            new("FullName", user.FullName)
        };
        if (mustChangePassword)
            claims.Add(new Claim("must_change_password", "true"));

        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var token = new JwtSecurityToken(
            issuer: _config.ValidIssuer,
            audience: _config.ValidAudience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_config.ExpirationInMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GenerateRefreshToken()
    {
        var randomNumber = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }
}
