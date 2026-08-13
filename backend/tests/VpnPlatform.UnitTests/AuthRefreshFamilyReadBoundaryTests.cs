using System.Data.Common;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using VpnPlatform.Api.Contracts;
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

public class AuthRefreshFamilyReadBoundaryTests
{
    [Fact]
    public async Task Reused_Modern_Refresh_Family_Should_Not_Walk_The_Rotation_Chain()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var interceptor = new CommandCaptureInterceptor();
        await using var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .AddInterceptors(interceptor)
            .Options);
        await db.Database.EnsureCreatedAsync();
        var now = new DateTimeOffset(2026, 8, 13, 11, 0, 0, TimeSpan.Zero);
        var user = new User
        {
            Email = "refresh-family-boundary@example.test",
            DisplayName = "Refresh Family Boundary",
            PasswordHash = "hash",
            RolesCsv = "User",
            Status = UserStatus.Active,
            ReferralCode = "REFRESHFAMILYBOUNDARY"
        };
        var familyId = Guid.NewGuid();
        var rawTokens = Enumerable.Range(0, 6).Select(index => $"refresh-family-token-{index}").ToArray();
        var sessions = rawTokens.Select((token, index) => new UserRefreshToken
        {
            UserId = user.Id,
            SessionVersion = user.SessionVersion,
            FamilyId = familyId,
            TokenHash = HashToken(token),
            ReplacedByTokenHash = index < rawTokens.Length - 1 ? HashToken(rawTokens[index + 1]) : string.Empty,
            RevokedAt = index < rawTokens.Length - 1 ? now.AddMinutes(index - 10) : null,
            RevocationReason = index < rawTokens.Length - 1 ? "rotated" : string.Empty,
            ExpiresAt = now.AddDays(1),
            CreatedAt = now.AddMinutes(index),
            UpdatedAt = now.AddMinutes(index)
        }).ToArray();
        db.Add(user);
        db.UserRefreshTokens.AddRange(sessions);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        interceptor.Commands.Clear();

        var result = await CreateController(db, now).Refresh(
            new RefreshTokenRequest(rawTokens[0]),
            CancellationToken.None);

        var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.Equal("refresh_token_reuse_detected", unauthorized.Value?.GetType().GetProperty("error")?.GetValue(unauthorized.Value));
        var refreshTokenReads = interceptor.Commands.Count(command =>
            command.TrimStart().StartsWith("SELECT", StringComparison.OrdinalIgnoreCase)
            && command.Contains("UserRefreshTokens", StringComparison.OrdinalIgnoreCase));
        Assert.True(refreshTokenReads <= 3, $"Expected at most 3 refresh-token reads, got {refreshTokenReads}.");
        Assert.Empty(await db.UserRefreshTokens.AsNoTracking().Where(x => x.RevokedAt == null).ToListAsync());
    }

    private static AuthController CreateController(ApplicationDbContext db, DateTimeOffset now)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Issuer"] = "vpn-platform-test",
                ["Jwt:Audience"] = "vpn-platform-test",
                ["Jwt:SigningKey"] = "unit-test-jwt-signing-key-0000000000000000000000",
                ["Security:SecretEncryptionKey"] = "unit-test-secret-encryption-key-000000000000000000",
                ["Auth:RefreshTokenDays"] = "30"
            })
            .Build();
        var controller = new AuthController(
            db,
            new PasswordService(),
            new JwtTokenService(configuration),
            new FixedClock(now),
            configuration,
            new SecretProtector(configuration));
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
        return controller;
    }

    private static string HashToken(string token)
        => Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

    private sealed class FixedClock(DateTimeOffset utcNow) : IClock
    {
        public DateTimeOffset UtcNow { get; } = utcNow;
    }

    private sealed class CommandCaptureInterceptor : DbCommandInterceptor
    {
        public List<string> Commands { get; } = [];

        public override InterceptionResult<DbDataReader> ReaderExecuting(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result)
        {
            Commands.Add(command.CommandText);
            return result;
        }

        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
        {
            Commands.Add(command.CommandText);
            return ValueTask.FromResult(result);
        }
    }
}
