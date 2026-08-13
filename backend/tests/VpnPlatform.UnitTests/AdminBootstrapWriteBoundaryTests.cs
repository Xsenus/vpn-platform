using System.Data.Common;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using VpnPlatform.Application.Abstractions;
using VpnPlatform.Application.Common;
using VpnPlatform.Domain.Entities;
using VpnPlatform.Domain.Enums;
using VpnPlatform.Infrastructure.Configuration;
using VpnPlatform.Infrastructure.Persistence;
using VpnPlatform.Infrastructure.Services;
using Xunit;

namespace VpnPlatform.UnitTests;

public class AdminBootstrapWriteBoundaryTests
{
    [Fact]
    public async Task Password_Reset_Should_Invalidate_Sessions_And_Reset_Tokens_With_Two_Bounded_Writes()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var interceptor = new CommandCaptureInterceptor();
        await using var db = CreateDb(connection, interceptor);
        await db.Database.EnsureCreatedAsync();
        var now = new DateTimeOffset(2026, 8, 13, 11, 30, 0, TimeSpan.Zero);
        var admin = await SeedAdminAsync(db, now, 25);
        db.ChangeTracker.Clear();
        interceptor.Commands.Clear();

        var result = await new AdminBootstrapService(new PasswordService(), new FixedClock(now)).BootstrapAsync(
            db,
            Options(),
            CancellationToken.None);

        Assert.False(result.Created);
        Assert.True(result.ExistingPasswordReset);
        Assert.DoesNotContain(interceptor.Commands, command =>
            command.TrimStart().StartsWith("SELECT", StringComparison.OrdinalIgnoreCase)
            && (command.Contains("UserRefreshTokens", StringComparison.OrdinalIgnoreCase)
                || command.Contains("PasswordResetTokens", StringComparison.OrdinalIgnoreCase)));
        Assert.Single(SessionUpdates(interceptor.Commands));
        Assert.Single(ResetTokenUpdates(interceptor.Commands));

        var updatedAdmin = await db.Users.AsNoTracking().SingleAsync();
        Assert.Equal(admin.Id, updatedAdmin.Id);
        Assert.Equal(1, updatedAdmin.SessionVersion);
        Assert.Equal(now, updatedAdmin.UpdatedAt);
        var resetState = await db.PasswordResetStates.AsNoTracking().SingleAsync();
        Assert.Equal(5, resetState.Generation);
        Assert.Equal(3, resetState.Revision);
        Assert.Equal(now, resetState.UpdatedAt);
        var sessions = await db.UserRefreshTokens.AsNoTracking().ToListAsync();
        Assert.All(sessions, session =>
        {
            Assert.Equal(now, session.RevokedAt);
            Assert.Equal("admin_bootstrap_session_invalidated", session.RevocationReason);
            Assert.Equal(1, session.Revision);
            Assert.Equal(now, session.UpdatedAt);
        });
        var resetTokens = await db.PasswordResetTokens.AsNoTracking().ToListAsync();
        Assert.All(resetTokens, token =>
        {
            Assert.Equal(now, token.InvalidatedAt);
            Assert.Equal("admin_bootstrap_password_reset", token.InvalidationReason);
            Assert.Equal(1, token.Revision);
            Assert.Equal(now, token.UpdatedAt);
        });
    }

    [Fact]
    public async Task Session_Write_Failure_Should_Roll_Back_Admin_And_Reset_Token_Changes()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var interceptor = new FailSessionUpdateInterceptor();
        await using var db = CreateDb(connection, interceptor);
        await db.Database.EnsureCreatedAsync();
        var now = new DateTimeOffset(2026, 8, 13, 11, 45, 0, TimeSpan.Zero);
        var admin = await SeedAdminAsync(db, now, 2);
        var originalPasswordHash = admin.PasswordHash;
        db.ChangeTracker.Clear();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new AdminBootstrapService(new PasswordService(), new FixedClock(now)).BootstrapAsync(
                db,
                Options(),
                CancellationToken.None));

        db.ChangeTracker.Clear();
        var persistedAdmin = await db.Users.AsNoTracking().SingleAsync();
        Assert.Equal(UserRoles.Admin, persistedAdmin.RolesCsv);
        Assert.Equal(0, persistedAdmin.SessionVersion);
        Assert.Equal(originalPasswordHash, persistedAdmin.PasswordHash);
        var resetState = await db.PasswordResetStates.AsNoTracking().SingleAsync();
        Assert.Equal(4, resetState.Generation);
        Assert.Equal(2, resetState.Revision);
        Assert.All(await db.PasswordResetTokens.AsNoTracking().ToListAsync(), token => Assert.Null(token.InvalidatedAt));
        Assert.All(await db.UserRefreshTokens.AsNoTracking().ToListAsync(), session => Assert.Null(session.RevokedAt));
    }

    private static async Task<User> SeedAdminAsync(ApplicationDbContext db, DateTimeOffset now, int count)
    {
        var admin = new User
        {
            Email = "admin-bootstrap-boundary@example.test",
            DisplayName = "Admin Bootstrap Boundary",
            PasswordHash = new PasswordService().Hash("OldAdminPassword123!"),
            RolesCsv = UserRoles.Admin,
            Status = UserStatus.Active,
            ReferralCode = "ADMINBOOTSTRAPBOUNDARY"
        };
        db.Users.Add(admin);
        db.PasswordResetStates.Add(new PasswordResetState
        {
            UserId = admin.Id,
            Generation = 4,
            Revision = 2
        });
        db.UserRefreshTokens.AddRange(Enumerable.Range(0, count).Select(index => new UserRefreshToken
        {
            UserId = admin.Id,
            SessionVersion = admin.SessionVersion,
            FamilyId = Guid.NewGuid(),
            TokenHash = $"admin-bootstrap-session-{index}",
            ExpiresAt = now.AddDays(1)
        }));
        db.PasswordResetTokens.AddRange(Enumerable.Range(0, count).Select(index => new PasswordResetToken
        {
            UserId = admin.Id,
            Generation = 4,
            TokenHash = $"admin-bootstrap-reset-{index}",
            ExpiresAt = now.AddMinutes(30)
        }));
        await db.SaveChangesAsync();
        return admin;
    }

    private static AdminBootstrapOptions Options()
        => new()
        {
            Enabled = true,
            Email = "admin-bootstrap-boundary@example.test",
            Password = "NewAdminPassword123!",
            RolesCsv = UserRoles.SuperAdmin,
            ResetExistingPassword = true
        };

    private static ApplicationDbContext CreateDb(SqliteConnection connection, DbCommandInterceptor interceptor)
        => new(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .AddInterceptors(interceptor)
            .Options);

    private static List<string> SessionUpdates(IEnumerable<string> commands)
        => commands.Where(command =>
            command.TrimStart().StartsWith("UPDATE", StringComparison.OrdinalIgnoreCase)
            && command.Contains("UserRefreshTokens", StringComparison.OrdinalIgnoreCase)).ToList();

    private static List<string> ResetTokenUpdates(IEnumerable<string> commands)
        => commands.Where(command =>
            command.TrimStart().StartsWith("UPDATE", StringComparison.OrdinalIgnoreCase)
            && command.Contains("PasswordResetTokens", StringComparison.OrdinalIgnoreCase)).ToList();

    private sealed class FixedClock(DateTimeOffset utcNow) : IClock
    {
        public DateTimeOffset UtcNow { get; } = utcNow;
    }

    private class CommandCaptureInterceptor : DbCommandInterceptor
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

    private sealed class FailSessionUpdateInterceptor : CommandCaptureInterceptor
    {
        public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            base.NonQueryExecutingAsync(command, eventData, result, cancellationToken);
            if (command.CommandText.TrimStart().StartsWith("UPDATE", StringComparison.OrdinalIgnoreCase)
                && command.CommandText.Contains("UserRefreshTokens", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Injected admin bootstrap session write failure.");
            }

            return ValueTask.FromResult(result);
        }
    }
}
