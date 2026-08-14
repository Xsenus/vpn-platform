using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using VpnPlatform.Application.Abstractions;
using VpnPlatform.Application.Common;
using VpnPlatform.Domain.Entities;
using VpnPlatform.Infrastructure.Services;

namespace VpnPlatform.Infrastructure.Auth;

public class JwtTokenService : ITokenService
{
    private readonly IConfiguration _configuration;
    private readonly IClock _clock;

    public JwtTokenService(IConfiguration configuration, IClock? clock = null)
    {
        _configuration = configuration;
        _clock = clock ?? new SystemClock();
    }

    public string CreateAccessToken(User user, IEnumerable<string> roles)
    {
        var signingKey = _configuration["Jwt:SigningKey"] ?? throw new InvalidOperationException("Jwt:SigningKey is required.");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new("name", user.DisplayName),
            new("preferred_language", user.PreferredLanguage),
            new(AuthClaimTypes.SessionVersion, user.SessionVersion.ToString(System.Globalization.CultureInfo.InvariantCulture))
        };

        claims.AddRange(roles.Select(x => new Claim(ClaimTypes.Role, x)));

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"] ?? "VpnPlatform",
            audience: _configuration["Jwt:Audience"] ?? "VpnPlatform.Clients",
            claims: claims,
            expires: _clock.UtcNow.UtcDateTime.AddMinutes(30),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string CreateRefreshToken()
    {
        var bytes = System.Security.Cryptography.RandomNumberGenerator.GetBytes(48);
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }
}
