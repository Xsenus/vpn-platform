using System.Data.Common;
using System.Text.Json;
using System.Security.Claims;
using Microsoft.Data.Sqlite;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using VpnPlatform.Api.Controllers.Admin;
using VpnPlatform.Api.Security;
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
        db.UserRefreshTokens.Add(new UserRefreshToken
        {
            UserId = userId,
            TokenHash = "active-refresh-session",
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(30)
        });
        await db.SaveChangesAsync();

        var updatedAt = await db.Users.Where(x => x.Id == userId).Select(x => x.UpdatedAt).SingleAsync();
        using var payload = JsonDocument.Parse(JsonSerializer.Serialize(new
        {
            displayName = "After",
            isBlocked = true,
            status = "Suspended",
            updatedAt
        }));
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
        Assert.Equal(1, updated.SessionVersion);
        var session = await db.UserRefreshTokens.SingleAsync(x => x.UserId == userId);
        Assert.NotNull(session.RevokedAt);
        Assert.Equal("admin_user_deactivated", session.RevocationReason);
        Assert.Equal(1, session.Revision);
        var audit = await db.AuditLogs.SingleAsync(x => x.Action == "user.update");
        Assert.Equal(userId.ToString(), audit.EntityId);
        Assert.NotEqual(audit.BeforeJson, audit.AfterJson);
        Assert.DoesNotContain("secret-hash", audit.BeforeJson, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Patch_Should_Reject_Unknown_Fields_Without_Mutating_User()
    {
        await using var db = CreateDbContext();
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "unknown-field@example.test",
            DisplayName = "Before",
            PasswordHash = "secret-hash",
            RolesCsv = UserRoles.User,
            Status = UserStatus.Active,
            ReferralCode = "UNKNOWN-FIELD"
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        using var payload = JsonDocument.Parse(JsonSerializer.Serialize(new
        {
            displayName = "After",
            updatedAt = user.UpdatedAt,
            displayNmae = "Typo"
        }));
        var result = await new AdminUsersController(db).Patch(user.Id, payload.RootElement, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
        var persisted = await db.Users.AsNoTracking().SingleAsync(x => x.Id == user.Id);
        Assert.Equal("Before", persisted.DisplayName);
        Assert.Empty(await db.AuditLogs.AsNoTracking().ToListAsync());
    }

    [Fact]
    public async Task Patch_Should_Reject_Stale_Profile_Version_Without_Overwriting_Newer_State()
    {
        await using var db = CreateDbContext();
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "stale-profile@example.test",
            DisplayName = "Before",
            PasswordHash = "secret-hash",
            RolesCsv = UserRoles.User,
            Status = UserStatus.Active,
            ReferralCode = "STALE-PROFILE"
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        var staleUpdatedAt = user.UpdatedAt;

        user.DisplayName = "Newer state";
        user.UpdatedAt = staleUpdatedAt.AddSeconds(1);
        await db.SaveChangesAsync();

        using var payload = JsonDocument.Parse(JsonSerializer.Serialize(new
        {
            status = UserStatus.Suspended.ToString(),
            updatedAt = staleUpdatedAt
        }));
        var result = await new AdminUsersController(db).Patch(user.Id, payload.RootElement, CancellationToken.None);

        Assert.IsType<ConflictObjectResult>(result);
        var persisted = await db.Users.AsNoTracking().SingleAsync(x => x.Id == user.Id);
        Assert.Equal("Newer state", persisted.DisplayName);
        Assert.Equal(UserStatus.Active, persisted.Status);
        Assert.Empty(await db.AuditLogs.AsNoTracking().ToListAsync());
    }

    [Fact]
    public async Task Patch_Should_Roll_Back_Session_Revocation_When_User_Changes_During_Save()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"vpn-admin-user-stale-save-{Guid.NewGuid():N}.db");
        try
        {
            var connectionString = $"Data Source={databasePath}";
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlite(connectionString)
                .Options;
            var userId = Guid.NewGuid();
            DateTimeOffset expectedUpdatedAt;
            await using (var seed = new ApplicationDbContext(options))
            {
                await seed.Database.EnsureCreatedAsync();
                var user = new User
                {
                    Id = userId,
                    Email = "stale-save@example.test",
                    DisplayName = "Before",
                    PasswordHash = "hash",
                    RolesCsv = UserRoles.User,
                    Status = UserStatus.Active,
                    ReferralCode = "STALE-SAVE"
                };
                seed.Users.Add(user);
                seed.UserRefreshTokens.Add(new UserRefreshToken
                {
                    UserId = userId,
                    TokenHash = "stale-save-token",
                    ExpiresAt = DateTimeOffset.UtcNow.AddDays(1)
                });
                await seed.SaveChangesAsync();
                expectedUpdatedAt = user.UpdatedAt;
            }

            var interceptor = new BeforeTransactionInterceptor(async () =>
            {
                await using var competitor = new ApplicationDbContext(options);
                var user = await competitor.Users.SingleAsync(x => x.Id == userId);
                user.DisplayName = "Concurrent winner";
                user.UpdatedAt = expectedUpdatedAt.AddSeconds(1);
                await competitor.SaveChangesAsync();
            });
            var raceOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlite(connectionString)
                .AddInterceptors(interceptor)
                .Options;
            await using var db = new ApplicationDbContext(raceOptions);
            using var payload = JsonDocument.Parse(JsonSerializer.Serialize(new
            {
                isBlocked = true,
                status = "Suspended",
                updatedAt = expectedUpdatedAt
            }));

            var action = await new AdminUsersController(db).Patch(userId, payload.RootElement, CancellationToken.None);

            Assert.IsType<ConflictObjectResult>(action);
            await using var verify = new ApplicationDbContext(options);
            var persistedUser = await verify.Users.AsNoTracking().SingleAsync(x => x.Id == userId);
            Assert.Equal("Concurrent winner", persistedUser.DisplayName);
            Assert.False(persistedUser.IsBlocked);
            Assert.Equal(UserStatus.Active, persistedUser.Status);
            Assert.Null((await verify.UserRefreshTokens.AsNoTracking().SingleAsync()).RevokedAt);
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
    public async Task Patch_Should_Retry_User_Deactivation_After_Concurrent_Session_Mutation()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"vpn-admin-user-session-race-{Guid.NewGuid():N}.db");
        try
        {
            var connectionString = $"Data Source={databasePath}";
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlite(connectionString)
                .Options;
            var userId = Guid.NewGuid();
            await using (var seed = new ApplicationDbContext(options))
            {
                await seed.Database.EnsureCreatedAsync();
                seed.Users.Add(new User
                {
                    Id = userId,
                    Email = "admin-session-race@example.test",
                    DisplayName = "Admin Session Race",
                    PasswordHash = "hash",
                    RolesCsv = UserRoles.User,
                    Status = UserStatus.Active,
                    ReferralCode = "admin-session-race"
                });
                seed.UserRefreshTokens.Add(new UserRefreshToken
                {
                    UserId = userId,
                    TokenHash = "admin-session-race-token",
                    ExpiresAt = DateTimeOffset.UtcNow.AddDays(1)
                });
                await seed.SaveChangesAsync();
            }

            var interceptor = new BeforeTransactionInterceptor(async () =>
            {
                await using var competitor = new ApplicationDbContext(options);
                var session = await competitor.UserRefreshTokens.SingleAsync();
                session.RevokedAt = DateTimeOffset.UtcNow;
                session.RevocationReason = "concurrent_refresh_rotation";
                session.Revision++;
                await competitor.SaveChangesAsync();
            });
            var raceOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlite(connectionString)
                .AddInterceptors(interceptor)
                .Options;
            await using var db = new ApplicationDbContext(raceOptions);
            var updatedAt = await db.Users.Where(x => x.Id == userId).Select(x => x.UpdatedAt).SingleAsync();
            using var payload = JsonDocument.Parse(JsonSerializer.Serialize(new
            {
                isBlocked = true,
                status = "Suspended",
                updatedAt
            }));
            var action = await new AdminUsersController(db).Patch(
                userId,
                payload.RootElement,
                CancellationToken.None);

            Assert.IsType<OkObjectResult>(action);
            await using var verify = new ApplicationDbContext(options);
            var user = await verify.Users.AsNoTracking().SingleAsync();
            Assert.True(user.IsBlocked);
            Assert.Equal(UserStatus.Suspended, user.Status);
            Assert.Equal(1, user.SessionVersion);
            Assert.Single(await verify.AuditLogs.AsNoTracking().Where(x => x.Action == "user.update").ToListAsync());
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
    public async Task Block_Then_Unblock_Should_Not_Reactivate_PreBlock_Access_Token()
    {
        await using var db = CreateDbContext();
        var userId = Guid.NewGuid();
        db.Users.Add(new User
        {
            Id = userId,
            Email = "session-version@example.test",
            DisplayName = "Session Version",
            PasswordHash = "hash",
            RolesCsv = UserRoles.User,
            Status = UserStatus.Active,
            ReferralCode = "SESSION-VERSION"
        });
        await db.SaveChangesAsync();
        var oldAccessPrincipal = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim("session_version", "0")
            ],
            "test"));
        var controller = new AdminUsersController(db);

        var updatedAt = await db.Users.Where(x => x.Id == userId).Select(x => x.UpdatedAt).SingleAsync();
        using (var blockPayload = JsonDocument.Parse(JsonSerializer.Serialize(new
        {
            isBlocked = true,
            status = "Suspended",
            updatedAt
        })))
        {
            Assert.IsType<OkObjectResult>(await controller.Patch(userId, blockPayload.RootElement, CancellationToken.None));
        }
        updatedAt = await db.Users.Where(x => x.Id == userId).Select(x => x.UpdatedAt).SingleAsync();
        using (var unblockPayload = JsonDocument.Parse(JsonSerializer.Serialize(new
        {
            isBlocked = false,
            status = "Active",
            updatedAt
        })))
        {
            Assert.IsType<OkObjectResult>(await controller.Patch(userId, unblockPayload.RootElement, CancellationToken.None));
        }

        Assert.False(await ActiveUserAccessValidator.IsActiveAsync(oldAccessPrincipal, db, CancellationToken.None));
    }

    [Theory]
    [InlineData("[]")]
    [InlineData("{\"status\":\"999\"}")]
    [InlineData("{\"status\":999}")]
    [InlineData("{\"isBlocked\":\"true\"}")]
    [InlineData("{\"displayName\":123}")]
    public async Task Patch_Should_Reject_Invalid_Field_Types_Without_Mutating_User(string rawPayload)
    {
        await using var db = CreateDbContext();
        var userId = Guid.NewGuid();
        db.Users.Add(new User
        {
            Id = userId,
            Email = "validation@example.test",
            DisplayName = "Before",
            PasswordHash = "secret-hash",
            RolesCsv = UserRoles.User,
            Status = UserStatus.Active,
            IsBlocked = false,
            ReferralCode = "REF4"
        });
        await db.SaveChangesAsync();
        using var payload = JsonDocument.Parse(rawPayload);

        var result = await new AdminUsersController(db).Patch(userId, payload.RootElement, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
        var user = await db.Users.AsNoTracking().SingleAsync(x => x.Id == userId);
        Assert.Equal("Before", user.DisplayName);
        Assert.False(user.IsBlocked);
        Assert.Equal(UserStatus.Active, user.Status);
        Assert.Empty(await db.AuditLogs.ToListAsync());
    }

    [Fact]
    public async Task GetList_Should_Reject_Undefined_Numeric_Status_Filter()
    {
        await using var db = CreateDbContext();

        var result = await new AdminUsersController(db).GetList(null, "999", null, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task GetOverview_Should_Return_Full_User_Profile_On_Sqlite()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var db = CreateSqliteDbContext(connection);
        await db.Database.EnsureCreatedAsync();

        var userId = Guid.NewGuid();
        var tariffId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var paymentId = Guid.NewGuid();
        var subscriptionId = Guid.NewGuid();
        var nodeId = Guid.NewGuid();
        var accessId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        db.Users.Add(new User
        {
            Id = userId,
            Email = "client@example.test",
            DisplayName = "Client",
            PasswordHash = "hash-that-must-not-leak",
            RolesCsv = UserRoles.User,
            Status = UserStatus.Active,
            PreferredLanguage = "ru",
            ReferralCode = "CLIENT1",
            AuthSource = AuthSource.Telegram,
            EmailConfirmed = true,
            CreatedAt = now.AddDays(-20),
            UpdatedAt = now.AddDays(-1)
        });
        db.Tariffs.Add(new Tariff
        {
            Id = tariffId,
            Name = "Premium",
            Slug = "premium",
            DurationDays = 30,
            Price = 490,
            Currency = "RUB",
            CreatedAt = now.AddDays(-30),
            UpdatedAt = now.AddDays(-30)
        });
        db.Orders.Add(new Order
        {
            Id = orderId,
            UserId = userId,
            TariffId = tariffId,
            Status = OrderStatus.Completed,
            Type = OrderType.NewSubscription,
            Amount = 490,
            Currency = "RUB",
            Channel = ChannelType.Web,
            PaymentProvider = PaymentProvider.YooKassa,
            ExpiresAt = now.AddDays(1),
            PaidAt = now.AddHours(-5),
            IsFirstPurchase = true,
            CreatedAt = now.AddHours(-6),
            UpdatedAt = now.AddHours(-5)
        });
        db.Payments.Add(new PaymentAttempt
        {
            Id = paymentId,
            OrderId = orderId,
            Provider = PaymentProvider.YooKassa,
            ProviderMode = PaymentProviderMode.Sandbox,
            ProviderPaymentId = "pay_1",
            ExternalEventId = "evt_1",
            IdempotencyKey = "idem_1",
            Amount = 490,
            Currency = "RUB",
            Status = PaymentStatus.Succeeded,
            ConfirmationUrl = "https://pay.example.test/confirm",
            ReturnUrl = "https://vpn.example.test/return",
            SignatureValidated = true,
            IsActivationProcessed = true,
            ActivationProcessedAt = now.AddHours(-4),
            PaidAt = now.AddHours(-5),
            CreatedAt = now.AddHours(-6),
            UpdatedAt = now.AddHours(-4)
        });
        db.Subscriptions.Add(new Subscription
        {
            Id = subscriptionId,
            UserId = userId,
            TariffId = tariffId,
            Status = SubscriptionStatus.Active,
            StartAt = now.AddHours(-4),
            EndAt = now.AddDays(26),
            AutoRenewFlag = true,
            SourceChannel = ChannelType.Web,
            CurrentServerId = nodeId,
            LastPaymentId = paymentId,
            RenewalCount = 1,
            CreatedAt = now.AddHours(-4),
            UpdatedAt = now.AddHours(-3)
        });
        db.VpnNodes.Add(new VpnNode
        {
            Id = nodeId,
            Name = "NL-1",
            Host = "nl1.example.test",
            IpAddress = "127.0.0.1",
            CreatedAt = now.AddDays(-10),
            UpdatedAt = now.AddDays(-1)
        });
        db.AccessCredentials.Add(new AccessCredential
        {
            Id = accessId,
            SubscriptionId = subscriptionId,
            ServerId = nodeId,
            ProviderType = "x3ui",
            ProviderAccessId = "client-1",
            AccessUri = "vless://client-1",
            QrCodePath = "vless://client-1",
            ConfigPath = "/configs/client-1.json",
            Status = AccessCredentialStatus.Active,
            IssuedAt = now.AddHours(-4),
            LastSyncedAt = now.AddHours(-1),
            Revision = 2,
            CreatedAt = now.AddHours(-4),
            UpdatedAt = now.AddHours(-1)
        });
        db.TelegramAccounts.Add(new TelegramAccount
        {
            UserId = userId,
            TelegramUserId = 777001,
            Username = "client_tg",
            FirstName = "Client",
            LastName = "Telegram",
            LanguageCode = "ru",
            LinkedAt = now.AddDays(-2),
            LastSeenAt = now.AddHours(-2),
            RegistrationCompletedAt = now.AddDays(-2),
            CreatedAt = now.AddDays(-2),
            UpdatedAt = now.AddHours(-2)
        });
        db.SupportConversations.Add(new SupportConversation
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TelegramUserId = 777001,
            Channel = "telegram",
            Status = "open",
            Subject = "Нужна помощь",
            InternalNote = "VIP",
            Revision = 3,
            CreatedAt = now.AddHours(-3),
            UpdatedAt = now.AddHours(-2)
        });
        await db.SaveChangesAsync();

        var savedSubscription = await db.Subscriptions.SingleAsync(x => x.Id == subscriptionId);
        savedSubscription.CurrentAccessId = accessId;
        await db.SaveChangesAsync();

        var result = await CreateController(db, UserRoles.Admin).GetOverview(userId, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var json = JsonSerializer.Serialize(ok.Value);
        Assert.DoesNotContain("hash-that-must-not-leak", json, StringComparison.OrdinalIgnoreCase);
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        Assert.Equal(userId, root.GetProperty("User").GetProperty("Id").GetGuid());
        Assert.Equal(777001, root.GetProperty("TelegramAccounts")[0].GetProperty("TelegramUserId").GetInt64());
        Assert.Equal(orderId, root.GetProperty("Orders")[0].GetProperty("Id").GetGuid());
        Assert.Equal("Completed", root.GetProperty("Orders")[0].GetProperty("Status").GetString());
        Assert.Equal(paymentId, root.GetProperty("Payments")[0].GetProperty("Id").GetGuid());
        Assert.True(root.GetProperty("Payments")[0].GetProperty("SignatureValidated").GetBoolean());
        Assert.Equal(subscriptionId, root.GetProperty("Subscriptions")[0].GetProperty("Id").GetGuid());
        Assert.True(root.GetProperty("Subscriptions")[0].GetProperty("AutoRenewFlag").GetBoolean());
        Assert.Equal("vless://client-1", root.GetProperty("AccessCredentials")[0].GetProperty("QrCodePath").GetString());
        Assert.Equal("VIP", root.GetProperty("SupportConversations")[0].GetProperty("InternalNote").GetString());
        Assert.Equal(3, root.GetProperty("SupportConversations")[0].GetProperty("Revision").GetInt32());

        savedSubscription.Status = SubscriptionStatus.Cancelled;
        savedSubscription.CancelledAt = now;
        await db.SaveChangesAsync();
        using var cancelledDocument = ToJson(await CreateController(db, UserRoles.Admin).GetOverview(userId, CancellationToken.None));
        var cancelledAccess = cancelledDocument.RootElement.GetProperty("AccessCredentials")[0];
        Assert.Equal("Cancelled", cancelledAccess.GetProperty("SubscriptionStatus").GetString());
        Assert.True(cancelledAccess.GetProperty("IsTerminal").GetBoolean());
        Assert.Equal(string.Empty, cancelledAccess.GetProperty("ProviderAccessId").GetString());
        Assert.Equal(string.Empty, cancelledAccess.GetProperty("AccessUri").GetString());
        Assert.Equal(string.Empty, cancelledAccess.GetProperty("QrCodePath").GetString());
        Assert.Equal(string.Empty, cancelledAccess.GetProperty("ConfigPath").GetString());

        savedSubscription.Status = SubscriptionStatus.GracePeriod;
        savedSubscription.CancelledAt = null;
        savedSubscription.EndAt = now.AddDays(-3);
        savedSubscription.GracePeriodEndAt = now;
        await db.SaveChangesAsync();
        using var expiredDocument = ToJson(await CreateController(db, UserRoles.Admin).GetOverview(userId, CancellationToken.None));
        var expiredAccess = expiredDocument.RootElement.GetProperty("AccessCredentials")[0];
        Assert.Equal("GracePeriod", expiredAccess.GetProperty("SubscriptionStatus").GetString());
        Assert.True(expiredAccess.GetProperty("IsTerminal").GetBoolean());
        Assert.Equal(string.Empty, expiredAccess.GetProperty("ProviderAccessId").GetString());
        Assert.Equal(string.Empty, expiredAccess.GetProperty("AccessUri").GetString());
        Assert.Equal(string.Empty, expiredAccess.GetProperty("QrCodePath").GetString());
        Assert.Equal(string.Empty, expiredAccess.GetProperty("ConfigPath").GetString());
        Assert.Equal(now, expiredAccess.GetProperty("ExpiryDate").GetDateTimeOffset());

        using var financeDocument = ToJson(await CreateController(db, UserRoles.FinanceManager).GetOverview(userId, CancellationToken.None));
        Assert.Equal(1, financeDocument.RootElement.GetProperty("Orders").GetArrayLength());
        Assert.Equal(1, financeDocument.RootElement.GetProperty("Payments").GetArrayLength());
        Assert.Equal(0, financeDocument.RootElement.GetProperty("SupportConversations").GetArrayLength());

        using var supportDocument = ToJson(await CreateController(db, UserRoles.SupportAgent).GetOverview(userId, CancellationToken.None));
        Assert.Equal(0, supportDocument.RootElement.GetProperty("Orders").GetArrayLength());
        Assert.Equal(0, supportDocument.RootElement.GetProperty("Payments").GetArrayLength());
        Assert.Equal(1, supportDocument.RootElement.GetProperty("SupportConversations").GetArrayLength());
    }

    private static AdminUsersController CreateController(ApplicationDbContext db, string role)
    {
        var identity = new ClaimsIdentity([new Claim(ClaimTypes.Role, role)], "test");
        return new AdminUsersController(db)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
            }
        };
    }

    private static JsonDocument ToJson(IActionResult result)
        => JsonDocument.Parse(JsonSerializer.Serialize(Assert.IsType<OkObjectResult>(result).Value));

    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new ApplicationDbContext(options);
    }

    private static ApplicationDbContext CreateSqliteDbContext(SqliteConnection connection)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .Options;
        return new ApplicationDbContext(options);
    }

    private sealed class BeforeTransactionInterceptor(Func<Task> beforeTransaction) : DbTransactionInterceptor
    {
        private int _injected;

        public override async ValueTask<InterceptionResult<DbTransaction>> TransactionStartingAsync(
            DbConnection connection,
            TransactionStartingEventData eventData,
            InterceptionResult<DbTransaction> result,
            CancellationToken cancellationToken = default)
        {
            if (Interlocked.Exchange(ref _injected, 1) == 0)
            {
                await beforeTransaction();
            }

            return result;
        }
    }
}
