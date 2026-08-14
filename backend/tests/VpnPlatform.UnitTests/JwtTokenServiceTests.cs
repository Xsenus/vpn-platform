using System.IdentityModel.Tokens.Jwt;
using Microsoft.Extensions.Configuration;
using VpnPlatform.Application.Abstractions;
using VpnPlatform.Domain.Entities;
using VpnPlatform.Infrastructure.Auth;
using Xunit;

namespace VpnPlatform.UnitTests;

public class JwtTokenServiceTests
{
    [Fact]
    public void Access_Token_Expiry_Should_Use_Injected_Clock()
    {
        var now = new DateTimeOffset(2034, 4, 5, 6, 7, 8, TimeSpan.Zero);
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Issuer"] = "vpn-platform-test",
                ["Jwt:Audience"] = "vpn-platform-test",
                ["Jwt:SigningKey"] = "unit-test-jwt-signing-key-0000000000000000000000"
            })
            .Build();
        var service = new JwtTokenService(configuration, new FixedClock(now));
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "clock@example.test",
            DisplayName = "Clock User",
            PreferredLanguage = "ru"
        };

        var encoded = service.CreateAccessToken(user, ["User"]);
        var token = new JwtSecurityTokenHandler().ReadJwtToken(encoded);

        Assert.Equal(now.UtcDateTime.AddMinutes(30), token.ValidTo);
    }

    private sealed class FixedClock(DateTimeOffset utcNow) : IClock
    {
        public DateTimeOffset UtcNow { get; } = utcNow;
    }
}
