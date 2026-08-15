using System.Data.Common;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using VpnPlatform.Api.Controllers.Admin;
using VpnPlatform.Application.Abstractions;
using VpnPlatform.Application.Common;
using VpnPlatform.Domain.Entities;
using VpnPlatform.Domain.Enums;
using VpnPlatform.Infrastructure.Persistence;
using Xunit;

namespace VpnPlatform.UnitTests;

public class AdminUserSessionRevocationReadBoundaryTests
{
    [Fact]
    public async Task Deactivate_User_Should_Revoke_Sessions_With_One_Write_And_No_Token_Read()
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
            Email = "admin-user-revocation-boundary@example.test",
            DisplayName = "Admin User Revocation Boundary",
            PasswordHash = "hash",
            RolesCsv = UserRoles.User,
            Status = UserStatus.Active,
            ReferralCode = "ADMINUSERREVOCATIONBOUNDARY"
        };
        db.Users.Add(user);
        db.UserRefreshTokens.AddRange(Enumerable.Range(0, 25).Select(index => new UserRefreshToken
        {
            UserId = user.Id,
            SessionVersion = user.SessionVersion,
            FamilyId = Guid.NewGuid(),
            TokenHash = $"admin-user-revocation-boundary-{index}",
            ExpiresAt = now.AddDays(1)
        }));
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        interceptor.Commands.Clear();

        using var payload = JsonDocument.Parse(JsonSerializer.Serialize(new
        {
            isBlocked = true,
            status = "Suspended",
            updatedAt = user.UpdatedAt
        }));
        var controller = new AdminUsersController(db, new FixedClock(now))
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
        var result = await controller.Patch(user.Id, payload.RootElement, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
        Assert.DoesNotContain(interceptor.Commands, command =>
            command.TrimStart().StartsWith("SELECT", StringComparison.OrdinalIgnoreCase)
            && command.Contains("UserRefreshTokens", StringComparison.OrdinalIgnoreCase));
        var sessionUpdates = interceptor.Commands.Where(command =>
            command.TrimStart().StartsWith("UPDATE", StringComparison.OrdinalIgnoreCase)
            && command.Contains("UserRefreshTokens", StringComparison.OrdinalIgnoreCase)).ToList();
        Assert.Single(sessionUpdates);

        var updatedUser = await db.Users.AsNoTracking().SingleAsync();
        Assert.True(updatedUser.IsBlocked);
        Assert.Equal(UserStatus.Suspended, updatedUser.Status);
        Assert.Equal(1, updatedUser.SessionVersion);
        Assert.Equal(now, updatedUser.UpdatedAt);
        var sessions = await db.UserRefreshTokens.AsNoTracking().ToListAsync();
        Assert.Equal(25, sessions.Count);
        Assert.All(sessions, session =>
        {
            Assert.Equal(now, session.RevokedAt);
            Assert.Equal("admin_user_deactivated", session.RevocationReason);
            Assert.Equal(1, session.Revision);
            Assert.Equal(now, session.UpdatedAt);
        });
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
