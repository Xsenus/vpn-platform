using System.Data.Common;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using VpnPlatform.Api.Controllers.Auth;
using VpnPlatform.Application.Abstractions;
using VpnPlatform.Domain.Entities;
using VpnPlatform.Domain.Enums;
using VpnPlatform.Infrastructure.Auth;
using VpnPlatform.Infrastructure.Persistence;
using VpnPlatform.Infrastructure.Security;
using VpnPlatform.Infrastructure.Services;
using Xunit;

namespace VpnPlatform.UnitTests;

public class AuthSessionRevocationReadBoundaryTests
{
    [Fact]
    public async Task Logout_All_Should_Revoke_Sessions_Without_Materializing_Them()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var interceptor = new CommandCaptureInterceptor();
        await using var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .AddInterceptors(interceptor)
            .Options);
        await db.Database.EnsureCreatedAsync();
        var now = new DateTimeOffset(2026, 8, 13, 12, 0, 0, TimeSpan.Zero);
        var user = new User
        {
            Email = "logout-all-boundary@example.test",
            DisplayName = "Logout All Boundary",
            PasswordHash = "hash",
            RolesCsv = "User",
            Status = UserStatus.Active,
            ReferralCode = "LOGOUTALLBOUNDARY"
        };
        db.Users.Add(user);
        db.UserRefreshTokens.AddRange(Enumerable.Range(0, 25).Select(index => new UserRefreshToken
        {
            UserId = user.Id,
            SessionVersion = user.SessionVersion,
            FamilyId = Guid.NewGuid(),
            TokenHash = $"logout-all-boundary-{index}",
            ExpiresAt = now.AddDays(1)
        }));
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        interceptor.Commands.Clear();

        var controller = CreateController(db, now, user.Id);
        var result = await controller.Logout(null, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
        Assert.DoesNotContain(interceptor.Commands, command =>
            command.TrimStart().StartsWith("SELECT", StringComparison.OrdinalIgnoreCase)
            && command.Contains("UserRefreshTokens", StringComparison.OrdinalIgnoreCase));
        var sessionUpdates = interceptor.Commands.Where(command =>
            command.TrimStart().StartsWith("UPDATE", StringComparison.OrdinalIgnoreCase)
            && command.Contains("UserRefreshTokens", StringComparison.OrdinalIgnoreCase)).ToList();
        Assert.Single(sessionUpdates);
        var sessions = await db.UserRefreshTokens.AsNoTracking().ToListAsync();
        Assert.Equal(25, sessions.Count);
        Assert.All(sessions, session =>
        {
            Assert.Equal(now, session.RevokedAt);
            Assert.Equal("logout_all_current_user", session.RevocationReason);
            Assert.Equal(1, session.Revision);
        });
        var audit = await db.AuditLogs.AsNoTracking().SingleAsync(x => x.Action == "auth.logout_all");
        Assert.Contains("\"sessionsRevoked\":25", audit.AfterJson, StringComparison.Ordinal);
    }

    private static AuthController CreateController(ApplicationDbContext db, DateTimeOffset now, Guid userId)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Issuer"] = "vpn-platform-test",
                ["Jwt:Audience"] = "vpn-platform-test",
                ["Jwt:SigningKey"] = "unit-test-jwt-signing-key-0000000000000000000000",
                ["Security:SecretEncryptionKey"] = "unit-test-secret-encryption-key-000000000000000000"
            })
            .Build();
        var controller = new AuthController(
            db,
            new PasswordService(),
            new JwtTokenService(configuration),
            new FixedClock(now),
            configuration,
            new SecretProtector(configuration));
        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim(ClaimTypes.NameIdentifier, userId.ToString())],
                "test"))
        };
        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
        return controller;
    }

    private sealed class FixedClock(DateTimeOffset utcNow) : IClock
    {
        public DateTimeOffset UtcNow { get; } = utcNow;
    }

    private sealed class CommandCaptureInterceptor : DbCommandInterceptor
    {
        public List<string> Commands { get; } = [];

        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
        {
            Commands.Add(command.CommandText);
            return ValueTask.FromResult(result);
        }

        public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            Commands.Add(command.CommandText);
            return ValueTask.FromResult(result);
        }
    }
}
