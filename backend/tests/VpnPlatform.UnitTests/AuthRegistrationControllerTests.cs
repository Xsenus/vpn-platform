using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
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
        Assert.StartsWith("REF-", user.ReferralCode, StringComparison.Ordinal);
        Assert.Equal(20, user.ReferralCode.Length);
        Assert.Matches("^REF-[0-9A-F]{16}$", user.ReferralCode);
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

    [Fact]
    public async Task Register_Should_Attribute_Valid_Referral_And_Reject_Unknown_Code_Without_User()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var db = CreateSqliteDbContext(connection);
        await db.Database.EnsureCreatedAsync();
        var referrer = new User
        {
            Email = "referrer@example.test",
            DisplayName = "Referrer",
            PasswordHash = "hash",
            ReferralCode = "REF-ATTRIBUTION123",
            Status = UserStatus.Active,
            RolesCsv = UserRoles.User
        };
        db.Users.Add(referrer);
        await db.SaveChangesAsync();
        var controller = CreateAuthController(db);

        var attributed = await controller.Register(
            new RegisterRequest("referred@example.test", "Password123!", "Referred", " ref-attribution123 "),
            CancellationToken.None);
        var rejected = await controller.Register(
            new RegisterRequest("unknown-referral@example.test", "Password123!", "Unknown", "MISSING"),
            CancellationToken.None);

        Assert.IsType<OkObjectResult>(attributed);
        AssertBadRequestError(rejected, "invalid_referral_code");
        var referred = await db.Users.SingleAsync(x => x.Email == "referred@example.test");
        var relationship = await db.ReferralRelationships.SingleAsync();
        Assert.Equal(referrer.Id, relationship.ReferrerUserId);
        Assert.Equal(referred.Id, relationship.ReferredUserId);
        Assert.Equal(ChannelType.Web, relationship.SourceChannel);
        Assert.Equal(2, await db.Users.CountAsync());
    }

    [Fact]
    public async Task Concurrent_Duplicate_Email_Should_Return_Email_Exists_Without_Partial_Session()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"vpn-auth-register-{Guid.NewGuid():N}.db");
        try
        {
            var connectionString = $"Data Source={databasePath};Default Timeout=30;Pooling=False";
            var baseOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlite(connectionString)
                .Options;
            await using (var seed = new ApplicationDbContext(baseOptions))
            {
                await seed.Database.EnsureCreatedAsync();
            }

            const string email = "registration-race@example.test";
            var raceOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlite(connectionString)
                .AddInterceptors(new ConcurrentRegistrationInterceptor(baseOptions, email))
                .Options;
            await using var db = new ApplicationDbContext(raceOptions);
            var controller = CreateAuthController(db);

            var result = await controller.Register(
                new RegisterRequest(email, "Password123!", "Race loser"),
                CancellationToken.None);

            AssertBadRequestError(result, "email_exists");
            await using var verify = new ApplicationDbContext(baseOptions);
            var persisted = Assert.Single(await verify.Users.AsNoTracking().ToListAsync());
            Assert.Equal(email, persisted.Email);
            Assert.Equal("Concurrent winner", persisted.DisplayName);
            Assert.Empty(await verify.UserRefreshTokens.AsNoTracking().ToListAsync());
            Assert.Empty(await verify.AuditLogs.AsNoTracking().ToListAsync());
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
    public async Task NonUnique_Registration_Persistence_Failure_Should_Not_Be_Masked_As_Email_Exists()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"auth-registration-failure-{Guid.NewGuid():N}")
            .Options;
        await using var db = new FailingRegistrationDbContext(options);
        var controller = CreateAuthController(db);

        var exception = await Assert.ThrowsAsync<DbUpdateException>(() => controller.Register(
            new RegisterRequest("storage-failure@example.test", "Password123!", "Storage failure"),
            CancellationToken.None));

        Assert.Contains("storage unavailable", exception.InnerException!.Message, StringComparison.OrdinalIgnoreCase);
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

    private sealed class ConcurrentRegistrationInterceptor(
        DbContextOptions<ApplicationDbContext> competitorOptions,
        string email) : SaveChangesInterceptor
    {
        private int _inserted;

        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            if (Interlocked.Exchange(ref _inserted, 1) != 0)
            {
                return result;
            }

            await using var competitor = new ApplicationDbContext(competitorOptions);
            competitor.Users.Add(new User
            {
                Email = email,
                DisplayName = "Concurrent winner",
                PasswordHash = "competitor-hash",
                RolesCsv = UserRoles.User,
                Status = UserStatus.Active,
                ReferralCode = $"race-{Guid.NewGuid():N}"
            });
            await competitor.SaveChangesAsync(cancellationToken);
            return result;
        }
    }

    private sealed class FailingRegistrationDbContext(DbContextOptions<ApplicationDbContext> options)
        : ApplicationDbContext(options)
    {
        public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
            => Task.FromException<int>(new DbUpdateException(
                "simulated registration persistence failure",
                new InvalidOperationException("storage unavailable")));
    }
}
