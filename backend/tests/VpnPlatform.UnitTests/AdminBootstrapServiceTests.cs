using Microsoft.EntityFrameworkCore;
using VpnPlatform.Application.Common;
using VpnPlatform.Domain.Entities;
using VpnPlatform.Domain.Enums;
using VpnPlatform.Infrastructure.Configuration;
using VpnPlatform.Infrastructure.Persistence;
using VpnPlatform.Infrastructure.Services;
using Xunit;

namespace VpnPlatform.UnitTests;

public class AdminBootstrapServiceTests
{
    [Fact]
    public async Task BootstrapAsync_Should_Create_Admin_With_Normalized_Email_And_Roles()
    {
        await using var db = CreateDb();
        var passwordService = new PasswordService();
        var service = new AdminBootstrapService(passwordService);

        var result = await service.BootstrapAsync(db, new AdminBootstrapOptions
        {
            Enabled = true,
            Email = " Admin@Example.TEST ",
            Password = "StrongAdminPassword123!",
            DisplayName = "Главный администратор",
            RolesCsv = "SuperAdmin,Admin"
        }, CancellationToken.None);
        await db.SaveChangesAsync();

        var admin = await db.Users.SingleAsync();
        Assert.True(result.Created);
        Assert.True(result.ExistingPasswordReset);
        Assert.Equal("admin@example.test", result.Email);
        Assert.Equal("admin@example.test", admin.Email);
        Assert.Equal("Главный администратор", admin.DisplayName);
        Assert.Equal(UserRoles.NormalizeCsv("SuperAdmin,Admin"), admin.RolesCsv);
        Assert.Equal(UserStatus.Active, admin.Status);
        Assert.False(admin.IsBlocked);
        Assert.True(passwordService.Verify("StrongAdminPassword123!", admin.PasswordHash));
    }

    [Fact]
    public async Task BootstrapAsync_Should_Unblock_Admin_And_Preserve_Password_By_Default()
    {
        await using var db = CreateDb();
        var passwordService = new PasswordService();
        var originalHash = passwordService.Hash("OldAdminPassword123!");
        var adminId = Guid.NewGuid();
        db.Users.Add(new User
        {
            Id = adminId,
            Email = "admin@example.test",
            DisplayName = "Old Admin",
            PasswordHash = originalHash,
            RolesCsv = UserRoles.User,
            Status = UserStatus.Suspended,
            IsBlocked = true,
            SessionVersion = 3,
            ReferralCode = "ADMOLD"
        });
        db.UserRefreshTokens.Add(new UserRefreshToken
        {
            UserId = adminId,
            TokenHash = "bootstrap-suspended-session",
            SessionVersion = 3,
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(30)
        });
        db.PasswordResetStates.Add(new PasswordResetState
        {
            UserId = adminId,
            Generation = 4,
            Revision = 2
        });
        db.PasswordResetTokens.Add(new PasswordResetToken
        {
            UserId = adminId,
            Generation = 4,
            TokenHash = "bootstrap-reset-token",
            ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(30)
        });
        await db.SaveChangesAsync();

        var service = new AdminBootstrapService(passwordService);
        var result = await service.BootstrapAsync(db, new AdminBootstrapOptions
        {
            Enabled = true,
            Email = "admin@example.test",
            Password = "NewAdminPassword123!",
            RolesCsv = UserRoles.SuperAdmin,
            ResetExistingPassword = false
        }, CancellationToken.None);
        await db.SaveChangesAsync();

        var admin = await db.Users.SingleAsync();
        Assert.False(result.Created);
        Assert.False(result.ExistingPasswordReset);
        Assert.Equal(UserRoles.SuperAdmin, admin.RolesCsv);
        Assert.Equal(UserStatus.Active, admin.Status);
        Assert.False(admin.IsBlocked);
        Assert.Equal(originalHash, admin.PasswordHash);
        Assert.True(passwordService.Verify("OldAdminPassword123!", admin.PasswordHash));
        Assert.False(passwordService.Verify("NewAdminPassword123!", admin.PasswordHash));
        Assert.Equal(4, admin.SessionVersion);
        Assert.Equal(4, (await db.PasswordResetStates.SingleAsync()).Generation);
        Assert.Null((await db.PasswordResetTokens.SingleAsync()).InvalidatedAt);
        var session = await db.UserRefreshTokens.SingleAsync();
        Assert.NotNull(session.RevokedAt);
        Assert.Equal("admin_bootstrap_session_invalidated", session.RevocationReason);
    }

    [Fact]
    public async Task BootstrapAsync_Should_Reset_Existing_Admin_Password_When_Explicitly_Requested()
    {
        await using var db = CreateDb();
        var passwordService = new PasswordService();
        var adminId = Guid.NewGuid();
        db.Users.Add(new User
        {
            Id = adminId,
            Email = "admin@example.test",
            DisplayName = "Admin",
            PasswordHash = passwordService.Hash("OldAdminPassword123!"),
            RolesCsv = UserRoles.Admin,
            Status = UserStatus.Active,
            ReferralCode = "ADMREF"
        });
        db.UserRefreshTokens.Add(new UserRefreshToken
        {
            UserId = adminId,
            TokenHash = "bootstrap-active-session",
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(30)
        });
        db.PasswordResetStates.Add(new PasswordResetState
        {
            UserId = adminId,
            Generation = 4,
            Revision = 2
        });
        db.PasswordResetTokens.Add(new PasswordResetToken
        {
            UserId = adminId,
            Generation = 4,
            TokenHash = "bootstrap-active-reset-token",
            ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(30)
        });
        await db.SaveChangesAsync();

        var service = new AdminBootstrapService(passwordService);
        var result = await service.BootstrapAsync(db, new AdminBootstrapOptions
        {
            Enabled = true,
            Email = "admin@example.test",
            Password = "NewAdminPassword123!",
            RolesCsv = UserRoles.SuperAdmin,
            ResetExistingPassword = true
        }, CancellationToken.None);
        await db.SaveChangesAsync();

        var admin = await db.Users.SingleAsync();
        Assert.False(result.Created);
        Assert.True(result.ExistingPasswordReset);
        Assert.True(passwordService.Verify("NewAdminPassword123!", admin.PasswordHash));
        Assert.False(passwordService.Verify("OldAdminPassword123!", admin.PasswordHash));
        Assert.Equal(1, admin.SessionVersion);
        var resetState = await db.PasswordResetStates.SingleAsync();
        Assert.Equal(5, resetState.Generation);
        Assert.Equal(3, resetState.Revision);
        var resetToken = await db.PasswordResetTokens.SingleAsync();
        Assert.NotNull(resetToken.InvalidatedAt);
        Assert.Equal("admin_bootstrap_password_reset", resetToken.InvalidationReason);
        Assert.Equal(1, resetToken.Revision);
        var session = await db.UserRefreshTokens.SingleAsync();
        Assert.NotNull(session.RevokedAt);
        Assert.Equal("admin_bootstrap_session_invalidated", session.RevocationReason);
    }

    [Fact]
    public async Task BootstrapAsync_Should_Reject_Short_Password()
    {
        await using var db = CreateDb();
        var service = new AdminBootstrapService(new PasswordService());

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => service.BootstrapAsync(db, new AdminBootstrapOptions
        {
            Enabled = true,
            Email = "admin@example.test",
            Password = "short",
            RolesCsv = UserRoles.SuperAdmin
        }, CancellationToken.None));

        Assert.Contains("at least 16 characters", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static ApplicationDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        return new ApplicationDbContext(options);
    }
}
