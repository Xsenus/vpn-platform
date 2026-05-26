using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VpnPlatform.Api.Controllers.Admin;
using VpnPlatform.Application.Common;
using VpnPlatform.Domain.Entities;
using VpnPlatform.Domain.Enums;
using VpnPlatform.Infrastructure.Persistence;
using Xunit;

namespace VpnPlatform.UnitTests;

public class AdminUsersControllerTests
{
    [Fact]
    public async Task GetList_Should_Not_Return_PasswordHash_Or_Secret_Fields()
    {
        await using var db = CreateDbContext();
        db.Users.Add(new User
        {
            Id = Guid.NewGuid(),
            Email = "admin@example.test",
            DisplayName = "Admin",
            PasswordHash = "hash-that-must-not-leak",
            RolesCsv = UserRoles.Admin,
            Status = UserStatus.Active,
            PreferredLanguage = "ru",
            ReferralCode = "REF1",
            MetadataJson = "{\"secret\":\"must-not-leak\"}"
        });
        await db.SaveChangesAsync();

        var result = await new AdminUsersController(db).GetList(null, null, null, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var json = JsonSerializer.Serialize(ok.Value);
        Assert.DoesNotContain("PasswordHash", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("hash-that-must-not-leak", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("must-not-leak", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("MetadataJson", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetById_Should_Return_Roles_Status_And_Safe_Profile_Fields()
    {
        await using var db = CreateDbContext();
        var userId = Guid.NewGuid();
        db.Users.Add(new User
        {
            Id = userId,
            Email = "operator@example.test",
            DisplayName = "Operator",
            PasswordHash = "secret-hash",
            RolesCsv = $"{UserRoles.Admin},{UserRoles.Operator}",
            Status = UserStatus.Suspended,
            IsBlocked = true,
            PreferredLanguage = "en",
            ReferralCode = "REF2",
            AuthSource = AuthSource.Local,
            EmailConfirmed = true
        });
        await db.SaveChangesAsync();

        var result = await new AdminUsersController(db).GetById(userId, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var json = JsonSerializer.Serialize(ok.Value);
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        Assert.Equal(userId, root.GetProperty("Id").GetGuid());
        Assert.Equal("operator@example.test", root.GetProperty("Email").GetString());
        Assert.Equal("Operator", root.GetProperty("DisplayName").GetString());
        Assert.Equal($"{UserRoles.Admin},{UserRoles.Operator}", root.GetProperty("RolesCsv").GetString());
        Assert.Equal(UserStatus.Suspended.ToString(), root.GetProperty("Status").GetString());
        Assert.True(root.GetProperty("IsBlocked").GetBoolean());
        Assert.True(root.GetProperty("EmailConfirmed").GetBoolean());
        Assert.False(root.TryGetProperty("PasswordHash", out _));
    }

    [Fact]
    public async Task Patch_Should_Return_Safe_Dto_After_Update()
    {
        await using var db = CreateDbContext();
        var userId = Guid.NewGuid();
        db.Users.Add(new User
        {
            Id = userId,
            Email = "user@example.test",
            DisplayName = "Before",
            PasswordHash = "secret-hash",
            RolesCsv = UserRoles.User,
            Status = UserStatus.Active,
            PreferredLanguage = "ru",
            ReferralCode = "REF3"
        });
        await db.SaveChangesAsync();

        using var payload = JsonDocument.Parse("{\"displayName\":\"After\",\"isBlocked\":true,\"status\":\"Suspended\"}");
        var result = await new AdminUsersController(db).Patch(userId, payload.RootElement, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var json = JsonSerializer.Serialize(ok.Value);
        Assert.Contains("After", json, StringComparison.Ordinal);
        Assert.DoesNotContain("PasswordHash", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("secret-hash", json, StringComparison.OrdinalIgnoreCase);
        var updated = await db.Users.SingleAsync(x => x.Id == userId);
        Assert.Equal("After", updated.DisplayName);
        Assert.True(updated.IsBlocked);
        Assert.Equal(UserStatus.Suspended, updated.Status);
    }

    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new ApplicationDbContext(options);
    }
}
