using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using VpnPlatform.Api.Contracts;
using VpnPlatform.Api.Controllers.Auth;
using VpnPlatform.Application.Abstractions;
using VpnPlatform.Application.Common;
using VpnPlatform.Domain.Entities;
using VpnPlatform.Domain.Enums;
using VpnPlatform.Infrastructure.Auth;
using VpnPlatform.Infrastructure.Persistence;
using VpnPlatform.Infrastructure.Services;
using Xunit;

namespace VpnPlatform.UnitTests;

public class AuthSessionControllerTests
{
    [Fact]
    public async Task Login_Refresh_Logout_Should_Work_With_Sqlite_And_Reject_Invalid_Sessions()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var db = CreateSqliteDbContext(connection);
        await db.Database.EnsureCreatedAsync();

        var passwordService = new PasswordService();
        var activePassword = "CorrectPassword123!";
        db.Users.AddRange(
            new User
            {
                Id = Guid.NewGuid(),
                Email = "session@example.test",
                DisplayName = "Session User",
                PasswordHash = passwordService.Hash(activePassword),
                RolesCsv = UserRoles.User,
                Status = UserStatus.Active,
                ReferralCode = "session-user"
            },
            new User
            {
                Id = Guid.NewGuid(),
                Email = "inactive@example.test",
                DisplayName = "Inactive User",
                PasswordHash = passwordService.Hash(activePassword),
                RolesCsv = UserRoles.User,
                Status = UserStatus.Suspended,
                ReferralCode = "inactive-user"
            });
        await db.SaveChangesAsync();

        var controller = CreateAuthController(db);

        var invalidPassword = await controller.Login(new LoginRequest("session@example.test", "wrong-password"), CancellationToken.None);
        AssertUnauthorizedError(invalidPassword, "invalid_credentials");

        var inactive = await controller.Login(new LoginRequest("inactive@example.test", activePassword), CancellationToken.None);
        AssertUnauthorizedError(inactive, "invalid_credentials");

        var emptyLogin = await controller.Login(null!, CancellationToken.None);
        AssertUnauthorizedError(emptyLogin, "invalid_credentials");

        var login = await controller.Login(new LoginRequest("SESSION@example.test", activePassword), CancellationToken.None);
        var loginResponse = Assert.IsType<AuthResponse>(Assert.IsType<OkObjectResult>(login).Value);
        Assert.Equal("session@example.test", loginResponse.Email);
        Assert.False(string.IsNullOrWhiteSpace(loginResponse.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(loginResponse.RefreshToken));
        Assert.Single(await db.UserRefreshTokens.Where(x => x.RevokedAt == null).ToListAsync());
        Assert.DoesNotContain(loginResponse.RefreshToken, JsonSerializer.Serialize(await db.UserRefreshTokens.ToListAsync()), StringComparison.Ordinal);
        Assert.NotNull((await db.Users.SingleAsync(x => x.Email == "session@example.test")).LastLoginAt);

        var refresh = await controller.Refresh(new RefreshTokenRequest(loginResponse.RefreshToken), CancellationToken.None);
        var refreshed = Assert.IsType<AuthResponse>(Assert.IsType<OkObjectResult>(refresh).Value);
        Assert.NotEqual(loginResponse.RefreshToken, refreshed.RefreshToken);
        Assert.Equal(2, await db.UserRefreshTokens.CountAsync());
        Assert.Single(await db.UserRefreshTokens.Where(x => x.RevokedAt == null).ToListAsync());

        var oldTokenReuse = await controller.Refresh(new RefreshTokenRequest(loginResponse.RefreshToken), CancellationToken.None);
        AssertUnauthorizedError(oldTokenReuse, "refresh_token_reuse_detected");
        Assert.Empty(await db.UserRefreshTokens.Where(x => x.RevokedAt == null).ToListAsync());

        var repeatLogin = await controller.Login(new LoginRequest("session@example.test", activePassword), CancellationToken.None);
        var repeatLoginResponse = Assert.IsType<AuthResponse>(Assert.IsType<OkObjectResult>(repeatLogin).Value);
        Assert.Single(await db.UserRefreshTokens.Where(x => x.RevokedAt == null).ToListAsync());

        var logout = await controller.Logout(new LogoutRequest(repeatLoginResponse.RefreshToken), CancellationToken.None);
        Assert.IsType<OkObjectResult>(logout);
        Assert.Empty(await db.UserRefreshTokens.Where(x => x.RevokedAt == null).ToListAsync());

        var refreshAfterLogout = await controller.Refresh(new RefreshTokenRequest(repeatLoginResponse.RefreshToken), CancellationToken.None);
        AssertUnauthorizedError(refreshAfterLogout, "refresh_token_reuse_detected");

        var emptyRefresh = await controller.Refresh(null!, CancellationToken.None);
        AssertUnauthorizedError(emptyRefresh, "invalid_refresh_token");
    }

    private static void AssertUnauthorizedError(IActionResult result, string expectedError)
    {
        var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result);
        var error = unauthorized.Value?.GetType().GetProperty("error")?.GetValue(unauthorized.Value)?.ToString();
        Assert.Equal(expectedError, error);
    }

    private static AuthController CreateAuthController(ApplicationDbContext db)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Issuer"] = "vpn-platform-test",
                ["Jwt:Audience"] = "vpn-platform-test",
                ["Jwt:SigningKey"] = "unit-test-jwt-signing-key-0000000000000000000000",
                ["Auth:RefreshTokenDays"] = "30",
                ["Auth:PasswordReset:ExpiryMinutes"] = "30",
                ["Auth:PasswordReset:ReturnTokenForValidation"] = "true"
            })
            .Build();
        var controller = new AuthController(db, new PasswordService(), new JwtTokenService(configuration), new TestClock(), configuration);
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
        return controller;
    }

    private static ApplicationDbContext CreateSqliteDbContext(SqliteConnection connection)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .Options;
        return new ApplicationDbContext(options);
    }

    private sealed class TestClock : IClock
    {
        public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
    }
}
