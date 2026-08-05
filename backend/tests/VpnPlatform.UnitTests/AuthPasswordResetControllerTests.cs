using System.Text.Json;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using VpnPlatform.Api.Contracts;
using VpnPlatform.Api.Controllers.Auth;
using VpnPlatform.Api.Security;
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
        var resetUser = await db.Users.SingleAsync();
        Assert.True(passwordService.Verify(newPassword, resetUser.PasswordHash));
        Assert.Equal(1, resetUser.SessionVersion);
        var oldAccessPrincipal = new ClaimsPrincipal(new ClaimsIdentity(
            new JwtSecurityTokenHandler().ReadJwtToken(loginBeforeResetResponse.AccessToken).Claims,
            "test"));
        Assert.False(await ActiveUserAccessValidator.IsActiveAsync(oldAccessPrincipal, db, CancellationToken.None));

        AssertUnauthorizedError(await controller.Login(new LoginRequest("reset-flow@example.test", oldPassword), CancellationToken.None), "invalid_credentials");
        var loginAfterReset = await controller.Login(new LoginRequest("reset-flow@example.test", newPassword), CancellationToken.None);
        var loginAfterResetResponse = Assert.IsType<AuthResponse>(Assert.IsType<OkObjectResult>(loginAfterReset).Value);
        var newAccessPrincipal = new ClaimsPrincipal(new ClaimsIdentity(
            new JwtSecurityTokenHandler().ReadJwtToken(loginAfterResetResponse.AccessToken).Claims,
            "test"));
        Assert.True(await ActiveUserAccessValidator.IsActiveAsync(newAccessPrincipal, db, CancellationToken.None));

        AssertUnauthorizedError(
            await controller.Refresh(new RefreshTokenRequest(loginBeforeResetResponse.RefreshToken), CancellationToken.None),
            "session_invalidated");
        Assert.Contains(await db.AuditLogs.ToListAsync(), x => x.Action == "auth.refresh_reuse_detected");
        var refreshedNewSession = await controller.Refresh(
            new RefreshTokenRequest(loginAfterResetResponse.RefreshToken),
            CancellationToken.None);
        Assert.IsType<AuthResponse>(Assert.IsType<OkObjectResult>(refreshedNewSession).Value);
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

    [Fact]
    public async Task Reissued_Reset_Token_Should_Invalidate_Previous_Code_Before_Use()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var db = CreateSqliteDbContext(connection);
        await db.Database.EnsureCreatedAsync();
        var passwordService = new PasswordService();
        db.Users.Add(new User
        {
            Email = "reissued-reset@example.test",
            DisplayName = "Reissued Reset",
            PasswordHash = passwordService.Hash("InitialPassword123!"),
            RolesCsv = UserRoles.User,
            Status = UserStatus.Active,
            ReferralCode = "reissued-reset"
        });
        await db.SaveChangesAsync();
        var controller = CreateAuthController(db, new TestClock(new DateTimeOffset(2026, 8, 5, 3, 0, 0, TimeSpan.Zero)));

        var first = Assert.IsType<ForgotPasswordResponse>(Assert.IsType<OkObjectResult>(
            await controller.ForgotPassword(new ForgotPasswordRequest("reissued-reset@example.test"), CancellationToken.None)).Value);
        var second = Assert.IsType<ForgotPasswordResponse>(Assert.IsType<OkObjectResult>(
            await controller.ForgotPassword(new ForgotPasswordRequest("reissued-reset@example.test"), CancellationToken.None)).Value);

        AssertBadRequestError(
            await controller.ResetPassword(new ResetPasswordRequest(first.ValidationResetToken!, "CompromisedPassword123!"), CancellationToken.None),
            "invalid_or_expired_reset_token");
        Assert.IsType<OkObjectResult>(await controller.ResetPassword(
            new ResetPasswordRequest(second.ValidationResetToken!, "NewestPassword123!"),
            CancellationToken.None));
        Assert.True(passwordService.Verify("NewestPassword123!", (await db.Users.SingleAsync()).PasswordHash));
        Assert.Equal(3, (await db.PasswordResetStates.SingleAsync()).Generation);
        var invalidated = Assert.Single(await db.PasswordResetTokens.Where(x => x.InvalidatedAt != null).ToListAsync());
        Assert.Equal("password_reset_reissued", invalidated.InvalidationReason);
        Assert.Equal(1, invalidated.Revision);
    }

    [Fact]
    public async Task Successful_Reset_Should_Invalidate_Other_Outstanding_Tokens()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var db = CreateSqliteDbContext(connection);
        await db.Database.EnsureCreatedAsync();
        var passwordService = new PasswordService();
        db.Users.Add(new User
        {
            Id = Guid.NewGuid(),
            Email = "multiple-reset@example.test",
            DisplayName = "Multiple Reset",
            PasswordHash = passwordService.Hash("InitialPassword123!"),
            RolesCsv = UserRoles.User,
            Status = UserStatus.Active,
            ReferralCode = "multiple-reset"
        });
        await db.SaveChangesAsync();
        var controller = CreateAuthController(db, new TestClock(new DateTimeOffset(2026, 8, 5, 2, 20, 0, TimeSpan.Zero)));
        var first = Assert.IsType<ForgotPasswordResponse>(Assert.IsType<OkObjectResult>(
            await controller.ForgotPassword(new ForgotPasswordRequest("multiple-reset@example.test"), CancellationToken.None)).Value);
        var second = Assert.IsType<ForgotPasswordResponse>(Assert.IsType<OkObjectResult>(
            await controller.ForgotPassword(new ForgotPasswordRequest("multiple-reset@example.test"), CancellationToken.None)).Value);
        var legacySibling = await db.PasswordResetTokens.SingleAsync(x => x.InvalidatedAt != null);
        legacySibling.InvalidatedAt = null;
        legacySibling.InvalidationReason = string.Empty;
        legacySibling.Revision = 0;
        await db.SaveChangesAsync();

        Assert.IsType<OkObjectResult>(await controller.ResetPassword(
            new ResetPasswordRequest(second.ValidationResetToken!, "SecondPassword123!"),
            CancellationToken.None));

        AssertBadRequestError(
            await controller.ResetPassword(
                new ResetPasswordRequest(first.ValidationResetToken!, "CompromisedPassword123!"),
                CancellationToken.None),
            "invalid_or_expired_reset_token");
        Assert.True(passwordService.Verify("SecondPassword123!", (await db.Users.SingleAsync()).PasswordHash));
        var tokens = await db.PasswordResetTokens.ToListAsync();
        var invalidatedToken = Assert.Single(tokens, x => x.InvalidatedAt is not null);
        Assert.Equal("password_reset_completed", invalidatedToken.InvalidationReason);
        Assert.Equal(1, invalidatedToken.Revision);
        var usedToken = Assert.Single(tokens, x => x.UsedAt is not null);
        Assert.Equal(1, usedToken.Revision);
    }

    [Fact]
    public async Task Concurrent_Forgot_Password_Should_Retry_With_Newer_Generation()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"vpn-password-reset-generation-{Guid.NewGuid():N}.db");
        try
        {
            var connectionString = $"Data Source={databasePath};Default Timeout=30;Pooling=False";
            var baseOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlite(connectionString)
                .Options;
            var userId = Guid.NewGuid();
            await using (var setup = new ApplicationDbContext(baseOptions))
            {
                await setup.Database.EnsureCreatedAsync();
                setup.Users.Add(new User
                {
                    Id = userId,
                    Email = "concurrent-forgot@example.test",
                    DisplayName = "Concurrent Forgot",
                    PasswordHash = new PasswordService().Hash("InitialPassword123!"),
                    RolesCsv = UserRoles.User,
                    Status = UserStatus.Active,
                    ReferralCode = "concurrent-forgot"
                });
                await setup.SaveChangesAsync();
            }

            var clock = new TestClock(new DateTimeOffset(2026, 8, 5, 3, 15, 0, TimeSpan.Zero));
            var raceOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlite(connectionString)
                .Options;
            await using var db = new ConcurrentForgotPasswordDbContext(raceOptions, baseOptions, userId, clock.UtcNow);
            var controller = CreateAuthController(db, clock);

            var action = await controller.ForgotPassword(
                new ForgotPasswordRequest("concurrent-forgot@example.test"),
                CancellationToken.None);

            var response = Assert.IsType<ForgotPasswordResponse>(Assert.IsType<OkObjectResult>(action).Value);
            Assert.False(string.IsNullOrWhiteSpace(response.ValidationResetToken));
            await using var verify = new ApplicationDbContext(baseOptions);
            var state = await verify.PasswordResetStates.AsNoTracking().SingleAsync();
            Assert.Equal(2, state.Generation);
            Assert.Equal(1, state.Revision);
            var tokens = await verify.PasswordResetTokens.AsNoTracking().OrderBy(x => x.Generation).ToListAsync();
            Assert.Equal(2, tokens.Count);
            Assert.Equal("password_reset_reissued", tokens[0].InvalidationReason);
            Assert.NotNull(tokens[0].InvalidatedAt);
            Assert.Equal(1, tokens[0].Revision);
            Assert.Equal(2, tokens[1].Generation);
            Assert.Null(tokens[1].InvalidatedAt);
            Assert.Equal(HashToken(response.ValidationResetToken!), tokens[1].TokenHash);
            Assert.Equal(2, await verify.OutboxMessages.CountAsync(x => x.Type == "password_reset_requested"));
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            foreach (var path in new[] { databasePath, databasePath + "-shm", databasePath + "-wal" })
            {
                if (File.Exists(path)) File.Delete(path);
            }
        }
    }

    [Fact]
    public async Task Concurrent_Reissue_Should_Prevent_Stale_Reset_Commit()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"vpn-password-reset-reissue-{Guid.NewGuid():N}.db");
        try
        {
            var connectionString = $"Data Source={databasePath};Default Timeout=30;Pooling=False";
            var baseOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlite(connectionString)
                .Options;
            var userId = Guid.NewGuid();
            var oldPassword = "InitialPassword123!";
            var passwordService = new PasswordService();
            var now = new DateTimeOffset(2026, 8, 5, 3, 30, 0, TimeSpan.Zero);
            await using (var setup = new ApplicationDbContext(baseOptions))
            {
                await setup.Database.EnsureCreatedAsync();
                setup.Users.Add(new User
                {
                    Id = userId,
                    Email = "reset-reissue-race@example.test",
                    DisplayName = "Reset Reissue Race",
                    PasswordHash = passwordService.Hash(oldPassword),
                    RolesCsv = UserRoles.User,
                    Status = UserStatus.Active,
                    ReferralCode = "reset-reissue-race"
                });
                setup.PasswordResetStates.Add(new PasswordResetState { UserId = userId, Generation = 1 });
                setup.PasswordResetTokens.Add(new PasswordResetToken
                {
                    UserId = userId,
                    Generation = 1,
                    TokenHash = HashToken("stale-reset-token"),
                    ExpiresAt = now.AddMinutes(30)
                });
                await setup.SaveChangesAsync();
            }

            var raceOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlite(connectionString)
                .Options;
            await using var db = new ConcurrentResetReissueDbContext(raceOptions, baseOptions, userId, now);
            var controller = CreateAuthController(db, new TestClock(now));

            var action = await controller.ResetPassword(
                new ResetPasswordRequest("stale-reset-token", "CompromisedPassword123!"),
                CancellationToken.None);

            AssertBadRequestError(action, "invalid_or_expired_reset_token");
            await using var verify = new ApplicationDbContext(baseOptions);
            var user = await verify.Users.AsNoTracking().SingleAsync();
            Assert.True(passwordService.Verify(oldPassword, user.PasswordHash));
            Assert.False(passwordService.Verify("CompromisedPassword123!", user.PasswordHash));
            var state = await verify.PasswordResetStates.AsNoTracking().SingleAsync();
            Assert.Equal(2, state.Generation);
            Assert.Equal(1, state.Revision);
            var tokens = await verify.PasswordResetTokens.AsNoTracking().OrderBy(x => x.Generation).ToListAsync();
            Assert.Equal(2, tokens.Count);
            Assert.NotNull(tokens[0].InvalidatedAt);
            Assert.Null(tokens[0].UsedAt);
            Assert.Equal("password_reset_reissued", tokens[0].InvalidationReason);
            Assert.Null(tokens[1].InvalidatedAt);
            Assert.Equal(2, tokens[1].Generation);
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            foreach (var path in new[] { databasePath, databasePath + "-shm", databasePath + "-wal" })
            {
                if (File.Exists(path)) File.Delete(path);
            }
        }
    }

    [Fact]
    public async Task Password_Reset_Revision_Should_Reject_Concurrent_Sibling_Commit()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .Options;
        var userId = Guid.NewGuid();
        var firstId = Guid.NewGuid();
        var secondId = Guid.NewGuid();
        await using (var setup = new ApplicationDbContext(options))
        {
            await setup.Database.EnsureCreatedAsync();
            setup.Users.Add(new User
            {
                Id = userId,
                Email = "concurrent-reset@example.test",
                DisplayName = "Concurrent Reset",
                PasswordHash = new PasswordService().Hash("InitialPassword123!"),
                RolesCsv = UserRoles.User,
                Status = UserStatus.Active,
                ReferralCode = "concurrent-reset"
            });
            setup.PasswordResetTokens.AddRange(
                new PasswordResetToken { Id = firstId, UserId = userId, TokenHash = "first-hash", ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(30) },
                new PasswordResetToken { Id = secondId, UserId = userId, TokenHash = "second-hash", ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(30) });
            await setup.SaveChangesAsync();
        }

        await using var firstContext = new ApplicationDbContext(options);
        await using var secondContext = new ApplicationDbContext(options);
        var firstWinner = await firstContext.PasswordResetTokens.SingleAsync(x => x.Id == firstId);
        var invalidatedSibling = await firstContext.PasswordResetTokens.SingleAsync(x => x.Id == secondId);
        var concurrentSibling = await secondContext.PasswordResetTokens.SingleAsync(x => x.Id == secondId);
        var now = DateTimeOffset.UtcNow;
        firstWinner.UsedAt = now;
        firstWinner.Revision++;
        invalidatedSibling.InvalidatedAt = now;
        invalidatedSibling.InvalidationReason = "password_reset_completed";
        invalidatedSibling.Revision++;
        concurrentSibling.UsedAt = now.AddSeconds(1);
        concurrentSibling.Revision++;

        await firstContext.SaveChangesAsync();
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => secondContext.SaveChangesAsync());

        await using var verification = new ApplicationDbContext(options);
        var storedSibling = await verification.PasswordResetTokens.SingleAsync(x => x.Id == secondId);
        Assert.NotNull(storedSibling.InvalidatedAt);
        Assert.Null(storedSibling.UsedAt);
        Assert.Equal(1, storedSibling.Revision);
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

    private static string HashToken(string token)
        => Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(token.Trim())));

    private sealed class TestClock(DateTimeOffset utcNow) : IClock
    {
        public DateTimeOffset UtcNow { get; private set; } = utcNow;

        public void Advance(TimeSpan value) => UtcNow = UtcNow.Add(value);
    }

    private sealed class ConcurrentForgotPasswordDbContext(
        DbContextOptions<ApplicationDbContext> options,
        DbContextOptions<ApplicationDbContext> competitorOptions,
        Guid userId,
        DateTimeOffset now) : ApplicationDbContext(options)
    {
        private int _inserted;

        public override async Task<int> SaveChangesAsync(
            bool acceptAllChangesOnSuccess,
            CancellationToken cancellationToken = default)
        {
            if (Interlocked.Exchange(ref _inserted, 1) == 0)
            {
                await using var competitor = new ApplicationDbContext(competitorOptions);
                var resetToken = new PasswordResetToken
                {
                    UserId = userId,
                    Generation = 1,
                    TokenHash = HashToken("competitor-reset-token"),
                    ExpiresAt = now.AddMinutes(30)
                };
                competitor.PasswordResetStates.Add(new PasswordResetState { UserId = userId, Generation = 1 });
                competitor.PasswordResetTokens.Add(resetToken);
                competitor.OutboxMessages.Add(new OutboxMessage
                {
                    Type = "password_reset_requested",
                    CorrelationId = resetToken.Id.ToString("N"),
                    PayloadJson = "{}"
                });
                await competitor.SaveChangesAsync(cancellationToken);
            }

            return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }
    }

    private sealed class ConcurrentResetReissueDbContext(
        DbContextOptions<ApplicationDbContext> options,
        DbContextOptions<ApplicationDbContext> competitorOptions,
        Guid userId,
        DateTimeOffset now) : ApplicationDbContext(options)
    {
        private int _inserted;

        public override async Task<int> SaveChangesAsync(
            bool acceptAllChangesOnSuccess,
            CancellationToken cancellationToken = default)
        {
            if (Interlocked.Exchange(ref _inserted, 1) == 0)
            {
                await using var competitor = new ApplicationDbContext(competitorOptions);
                var state = await competitor.PasswordResetStates.SingleAsync(x => x.UserId == userId, cancellationToken);
                state.Generation = checked(state.Generation + 1);
                state.Revision = checked(state.Revision + 1);
                state.UpdatedAt = now;
                var previousToken = await competitor.PasswordResetTokens.SingleAsync(x => x.UserId == userId, cancellationToken);
                previousToken.InvalidatedAt = now;
                previousToken.InvalidationReason = "password_reset_reissued";
                previousToken.Revision = checked(previousToken.Revision + 1);
                previousToken.UpdatedAt = now;
                var nextToken = new PasswordResetToken
                {
                    UserId = userId,
                    Generation = state.Generation,
                    TokenHash = HashToken("newer-reset-token"),
                    ExpiresAt = now.AddMinutes(30)
                };
                competitor.PasswordResetTokens.Add(nextToken);
                competitor.OutboxMessages.Add(new OutboxMessage
                {
                    Type = "password_reset_requested",
                    CorrelationId = nextToken.Id.ToString("N"),
                    PayloadJson = "{}"
                });
                await competitor.SaveChangesAsync(cancellationToken);
            }

            return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }
    }
}
