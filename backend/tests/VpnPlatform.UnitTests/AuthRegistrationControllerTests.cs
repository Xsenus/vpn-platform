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
using VpnPlatform.Domain.Enums;
using VpnPlatform.Infrastructure.Auth;
using VpnPlatform.Infrastructure.Persistence;
using VpnPlatform.Infrastructure.Services;
using Xunit;

namespace VpnPlatform.UnitTests;

public class AuthRegistrationControllerTests
{
    [Fact]
    public async Task Register_Should_Create_User_And_Reject_Duplicate_Weak_Password_And_Invalid_Email_On_Sqlite()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var db = CreateSqliteDbContext(connection);
        await db.Database.EnsureCreatedAsync();

        var controller = CreateAuthController(db);

        var register = await controller.Register(new RegisterRequest("User@Example.Test", "Password123!", "  Иван  "), CancellationToken.None);
        var response = Assert.IsType<AuthResponse>(Assert.IsType<OkObjectResult>(register).Value);

        Assert.Equal("user@example.test", response.Email);
        Assert.Equal("Иван", response.DisplayName);
        Assert.False(string.IsNullOrWhiteSpace(response.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(response.RefreshToken));
        Assert.Single(await db.Users.ToListAsync());
        var user = await db.Users.SingleAsync();
        Assert.Equal("user@example.test", user.Email);
        Assert.Equal("Иван", user.DisplayName);
        Assert.Equal(UserStatus.Active, user.Status);
        Assert.Equal(UserRoles.User, user.RolesCsv);
        Assert.NotEmpty(user.ReferralCode);
        Assert.Single(await db.UserRefreshTokens.ToListAsync());
        Assert.DoesNotContain(response.RefreshToken, JsonSerializer.Serialize(await db.UserRefreshTokens.ToListAsync()), StringComparison.Ordinal);

        var duplicate = await controller.Register(new RegisterRequest(" USER@example.test ", "Password123!", "Duplicate"), CancellationToken.None);
        AssertBadRequestError(duplicate, "email_exists");

        var weakPassword = await controller.Register(new RegisterRequest("weak@example.test", "short", "Weak"), CancellationToken.None);
        AssertBadRequestError(weakPassword, "invalid_registration_request");

        var invalidEmail = await controller.Register(new RegisterRequest("name.surname@localhost", "Password123!", "Invalid"), CancellationToken.None);
        AssertBadRequestError(invalidEmail, "invalid_registration_request");

        var fallbackName = await controller.Register(new RegisterRequest("fallback@example.test", "Password123!", null!), CancellationToken.None);
        var fallbackResponse = Assert.IsType<AuthResponse>(Assert.IsType<OkObjectResult>(fallbackName).Value);
        Assert.Equal("fallback", fallbackResponse.DisplayName);
        Assert.Equal(2, await db.Users.CountAsync());
    }

    private static void AssertBadRequestError(IActionResult result, string expectedError)
    {
        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        var error = badRequest.Value?.GetType().GetProperty("error")?.GetValue(badRequest.Value)?.ToString();
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
