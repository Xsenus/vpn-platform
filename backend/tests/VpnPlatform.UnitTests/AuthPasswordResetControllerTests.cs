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

public class AuthPasswordResetControllerTests
{
    [Fact]
    public async Task Forgot_And_Reset_Password_Should_Work_With_Sqlite_And_Revoke_Old_Sessions()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var db = CreateSqliteDbContext(connection);
        await db.Database.EnsureCreatedAsync();

        var clock = new TestClock(new DateTimeOffset(2026, 6, 10, 10, 0, 0, TimeSpan.Zero));
        var passwordService = new PasswordService();
        var oldPassword = "OldPassword123!";
        var newPassword = "NewPassword123!";
        db.Users.Add(new User
        {
            Id = Guid.NewGuid(),
            Email = "reset-flow@example.test",
            DisplayName = "Reset Flow",
            PasswordHash = passwordService.Hash(oldPassword),
            RolesCsv = UserRoles.User,
            Status = UserStatus.Active,
            ReferralCode = "reset-flow"
        });
        await db.SaveChangesAsync();

        var controller = CreateAuthController(db, clock);
        var loginBeforeReset = await controller.Login(new LoginRequest("reset-flow@example.test", oldPassword), CancellationToken.None);
        var loginBeforeResetResponse = Assert.IsType<AuthResponse>(Assert.IsType<OkObjectResult>(loginBeforeReset).Value);
        Assert.False(string.IsNullOrWhiteSpace(loginBeforeResetResponse.RefreshToken));
        Assert.Single(await db.UserRefreshTokens.Where(x => x.RevokedAt == null).ToListAsync());

        var unknownForgot = await controller.ForgotPassword(new ForgotPasswordRequest("unknown@example.test"), CancellationToken.None);
        var unknownResponse = Assert.IsType<ForgotPasswordResponse>(Assert.IsType<OkObjectResult>(unknownForgot).Value);
        Assert.True(unknownResponse.Accepted);
        Assert.Null(unknownResponse.ValidationResetToken);
        Assert.Empty(await db.PasswordResetTokens.ToListAsync());
        Assert.Empty(await db.OutboxMessages.ToListAsync());

        var nullForgot = await controller.ForgotPassword(null!, CancellationToken.None);
        Assert.True(Assert.IsType<ForgotPasswordResponse>(Assert.IsType<OkObjectResult>(nullForgot).Value).Accepted);
        Assert.Empty(await db.PasswordResetTokens.ToListAsync());

        var forgot = await controller.ForgotPassword(new ForgotPasswordRequest("RESET-FLOW@example.test"), CancellationToken.None);
        var forgotResponse = Assert.IsType<ForgotPasswordResponse>(Assert.IsType<OkObjectResult>(forgot).Value);
        Assert.True(forgotResponse.Accepted);
        Assert.False(string.IsNullOrWhiteSpace(forgotResponse.ValidationResetToken));
        Assert.Single(await db.PasswordResetTokens.ToListAsync());
        Assert.Single(await db.OutboxMessages.Where(x => x.Type == "password_reset_requested").ToListAsync());
        Assert.DoesNotContain(forgotResponse.ValidationResetToken!, JsonSerializer.Serialize(await db.PasswordResetTokens.ToListAsync()), StringComparison.Ordinal);

        AssertBadRequestError(
            await controller.ResetPassword(new ResetPasswordRequest(forgotResponse.ValidationResetToken!, "short"), CancellationToken.None),
            "invalid_reset_request");
        AssertBadRequestError(await controller.ResetPassword(null!, CancellationToken.None), "invalid_reset_request");

        var reset = await controller.ResetPassword(new ResetPasswordRequest(forgotResponse.ValidationResetToken!, newPassword), CancellationToken.None);
        Assert.IsType<OkObjectResult>(reset);
        var storedReset = await db.PasswordResetTokens.SingleAsync();
        Assert.NotNull(storedReset.UsedAt);
        Assert.Empty(await db.UserRefreshTokens.Where(x => x.RevokedAt == null).ToListAsync());
        Assert.All(await db.UserRefreshTokens.ToListAsync(), session => Assert.Equal("password_reset", session.RevocationReason));
        Assert.True(passwordService.Verify(newPassword, (await db.Users.SingleAsync()).PasswordHash));

        AssertUnauthorizedError(await controller.Login(new LoginRequest("reset-flow@example.test", oldPassword), CancellationToken.None), "invalid_credentials");
        var loginAfterReset = await controller.Login(new LoginRequest("reset-flow@example.test", newPassword), CancellationToken.None);
        Assert.IsType<AuthResponse>(Assert.IsType<OkObjectResult>(loginAfterReset).Value);

        AssertBadRequestError(
            await controller.ResetPassword(new ResetPasswordRequest(forgotResponse.ValidationResetToken!, "AnotherPassword123!"), CancellationToken.None),
            "invalid_or_expired_reset_token");

        var secondForgot = await controller.ForgotPassword(new ForgotPasswordRequest("reset-flow@example.test"), CancellationToken.None);
        var secondToken = Assert.IsType<ForgotPasswordResponse>(Assert.IsType<OkObjectResult>(secondForgot).Value).ValidationResetToken;
        Assert.False(string.IsNullOrWhiteSpace(secondToken));
        var resetMessages = await db.OutboxMessages
            .Where(x => x.Type == "password_reset_requested")
            .ToListAsync();
        Assert.Equal(2, resetMessages.Count);
        Assert.Equal(2, resetMessages.Select(x => x.CorrelationId).Distinct(StringComparer.Ordinal).Count());
        clock.Advance(TimeSpan.FromMinutes(31));
        AssertBadRequestError(
            await controller.ResetPassword(new ResetPasswordRequest(secondToken!, "ExpiredPassword123!"), CancellationToken.None),
            "invalid_or_expired_reset_token");
    }

    [Fact]
    public async Task ResetPassword_Should_Reject_Token_When_User_Becomes_Inactive()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var db = CreateSqliteDbContext(connection);
        await db.Database.EnsureCreatedAsync();

        db.Users.Add(new User
        {
            Id = Guid.NewGuid(),
            Email = "inactive-reset@example.test",
            DisplayName = "Inactive Reset",
            PasswordHash = new PasswordService().Hash("OldPassword123!"),
            RolesCsv = UserRoles.User,
            Status = UserStatus.Active,
            ReferralCode = "inactive-reset"
        });
        await db.SaveChangesAsync();

        var controller = CreateAuthController(db, new TestClock(DateTimeOffset.UtcNow));
        var forgot = await controller.ForgotPassword(new ForgotPasswordRequest("inactive-reset@example.test"), CancellationToken.None);
        var token = Assert.IsType<ForgotPasswordResponse>(Assert.IsType<OkObjectResult>(forgot).Value).ValidationResetToken;
        Assert.False(string.IsNullOrWhiteSpace(token));

        var user = await db.Users.SingleAsync();
        user.Status = UserStatus.Suspended;
        await db.SaveChangesAsync();

        AssertBadRequestError(
            await controller.ResetPassword(new ResetPasswordRequest(token!, "NewPassword123!"), CancellationToken.None),
            "invalid_or_expired_reset_token");
    }

    private static void AssertBadRequestError(IActionResult result, string expectedError)
    {
        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        var error = badRequest.Value?.GetType().GetProperty("error")?.GetValue(badRequest.Value)?.ToString();
        Assert.Equal(expectedError, error);
    }

    private static void AssertUnauthorizedError(IActionResult result, string expectedError)
    {
        var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result);
        var error = unauthorized.Value?.GetType().GetProperty("error")?.GetValue(unauthorized.Value)?.ToString();
        Assert.Equal(expectedError, error);
    }

    private static AuthController CreateAuthController(ApplicationDbContext db, TestClock clock)
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
        var controller = new AuthController(db, new PasswordService(), new JwtTokenService(configuration), clock, configuration);
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

    private sealed class TestClock(DateTimeOffset utcNow) : IClock
    {
        public DateTimeOffset UtcNow { get; private set; } = utcNow;

        public void Advance(TimeSpan value) => UtcNow = UtcNow.Add(value);
    }
}
