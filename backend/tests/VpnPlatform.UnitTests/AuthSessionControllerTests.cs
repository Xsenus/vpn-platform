using System.Security.Claims;
using System.Security.Cryptography;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using System.Text.Json;
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

public class AuthSessionControllerTests
{
    [Fact]
    public void Api_Jwt_Bearer_Should_Revalidate_Current_User_State()
    {
        var root = FindRepositoryRoot();
        var program = File.ReadAllText(Path.Combine(root, "backend", "src", "VpnPlatform.Api", "Program.cs"));

        Assert.Contains("OnTokenValidated", program, StringComparison.Ordinal);
        Assert.Contains("ActiveUserAccessValidator.ValidateAsync", program, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Active_User_Access_Validator_Should_Reject_Blocked_Suspended_Missing_And_Invalid_Subjects_On_Sqlite()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var db = CreateSqliteDbContext(connection);
        await db.Database.EnsureCreatedAsync();

        var activeId = Guid.NewGuid();
        var blockedId = Guid.NewGuid();
        var suspendedId = Guid.NewGuid();
        db.Users.AddRange(
            new User { Id = activeId, Email = "active-token@example.test", DisplayName = "Active", ReferralCode = "active-token", Status = UserStatus.Active },
            new User { Id = blockedId, Email = "blocked-token@example.test", DisplayName = "Blocked", ReferralCode = "blocked-token", Status = UserStatus.Active, IsBlocked = true },
            new User { Id = suspendedId, Email = "suspended-token@example.test", DisplayName = "Suspended", ReferralCode = "suspended-token", Status = UserStatus.Suspended });
        await db.SaveChangesAsync();

        Assert.True(await ActiveUserAccessValidator.IsActiveAsync(Principal(activeId.ToString()), db, CancellationToken.None));
        Assert.False(await ActiveUserAccessValidator.IsActiveAsync(Principal(activeId.ToString(), 1), db, CancellationToken.None));
        Assert.False(await ActiveUserAccessValidator.IsActiveAsync(
            new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, activeId.ToString())], "test")),
            db,
            CancellationToken.None));
        Assert.False(await ActiveUserAccessValidator.IsActiveAsync(Principal(blockedId.ToString()), db, CancellationToken.None));
        Assert.False(await ActiveUserAccessValidator.IsActiveAsync(Principal(suspendedId.ToString()), db, CancellationToken.None));
        Assert.False(await ActiveUserAccessValidator.IsActiveAsync(Principal(Guid.NewGuid().ToString()), db, CancellationToken.None));
        Assert.False(await ActiveUserAccessValidator.IsActiveAsync(Principal("invalid-subject"), db, CancellationToken.None));
        Assert.False(await ActiveUserAccessValidator.IsActiveAsync(null, db, CancellationToken.None));
    }

    [Fact]
    public async Task Login_Refresh_Logout_Should_Work_With_Sqlite_And_Reject_Invalid_Sessions()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var db = CreateSqliteDbContext(connection);
        await db.Database.EnsureCreatedAsync();

        var passwordService = new PasswordService();
        var activePassword = "CorrectPassword123!";
        db.Users.AddRange(
            new User
            {
                Id = Guid.NewGuid(),
                Email = "session@example.test",
                DisplayName = "Session User",
                PasswordHash = passwordService.Hash(activePassword),
                RolesCsv = UserRoles.User,
                Status = UserStatus.Active,
                ReferralCode = "session-user"
            },
            new User
            {
                Id = Guid.NewGuid(),
                Email = "inactive@example.test",
                DisplayName = "Inactive User",
                PasswordHash = passwordService.Hash(activePassword),
                RolesCsv = UserRoles.User,
                Status = UserStatus.Suspended,
                ReferralCode = "inactive-user"
            });
        await db.SaveChangesAsync();

        var controller = CreateAuthController(db);

        var invalidPassword = await controller.Login(new LoginRequest("session@example.test", "wrong-password"), CancellationToken.None);
        AssertUnauthorizedError(invalidPassword, "invalid_credentials");

        var inactive = await controller.Login(new LoginRequest("inactive@example.test", activePassword), CancellationToken.None);
        AssertUnauthorizedError(inactive, "invalid_credentials");

        var emptyLogin = await controller.Login(null!, CancellationToken.None);
        AssertUnauthorizedError(emptyLogin, "invalid_credentials");

        var login = await controller.Login(new LoginRequest("SESSION@example.test", activePassword), CancellationToken.None);
        var loginResponse = Assert.IsType<AuthResponse>(Assert.IsType<OkObjectResult>(login).Value);
        Assert.Equal("session@example.test", loginResponse.Email);
        Assert.False(string.IsNullOrWhiteSpace(loginResponse.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(loginResponse.RefreshToken));
        var accessToken = new JwtSecurityTokenHandler().ReadJwtToken(loginResponse.AccessToken);
        Assert.Equal("0", accessToken.Claims.Single(x => x.Type == "session_version").Value);
        Assert.Single(await db.UserRefreshTokens.Where(x => x.RevokedAt == null).ToListAsync());
        Assert.DoesNotContain(loginResponse.RefreshToken, JsonSerializer.Serialize(await db.UserRefreshTokens.ToListAsync()), StringComparison.Ordinal);
        Assert.NotNull((await db.Users.SingleAsync(x => x.Email == "session@example.test")).LastLoginAt);

        var refresh = await controller.Refresh(new RefreshTokenRequest(loginResponse.RefreshToken), CancellationToken.None);
        var refreshed = Assert.IsType<AuthResponse>(Assert.IsType<OkObjectResult>(refresh).Value);
        Assert.NotEqual(loginResponse.RefreshToken, refreshed.RefreshToken);
        Assert.Equal(2, await db.UserRefreshTokens.CountAsync());
        Assert.Single(await db.UserRefreshTokens.Where(x => x.RevokedAt == null).ToListAsync());
        var rotatedFamily = await db.UserRefreshTokens.Select(x => x.FamilyId).Distinct().ToListAsync();
        Assert.Single(rotatedFamily);
        Assert.NotNull(rotatedFamily[0]);

        var oldTokenReuse = await controller.Refresh(new RefreshTokenRequest(loginResponse.RefreshToken), CancellationToken.None);
        AssertUnauthorizedError(oldTokenReuse, "refresh_token_reuse_detected");
        Assert.Empty(await db.UserRefreshTokens.Where(x => x.RevokedAt == null).ToListAsync());

        var repeatLogin = await controller.Login(new LoginRequest("session@example.test", activePassword), CancellationToken.None);
        var repeatLoginResponse = Assert.IsType<AuthResponse>(Assert.IsType<OkObjectResult>(repeatLogin).Value);
        Assert.Single(await db.UserRefreshTokens.Where(x => x.RevokedAt == null).ToListAsync());
        Assert.Equal(2, await db.UserRefreshTokens.Select(x => x.FamilyId).Distinct().CountAsync());

        var logout = await controller.Logout(new LogoutRequest(repeatLoginResponse.RefreshToken), CancellationToken.None);
        Assert.IsType<OkObjectResult>(logout);
        Assert.Empty(await db.UserRefreshTokens.Where(x => x.RevokedAt == null).ToListAsync());

        var refreshAfterLogout = await controller.Refresh(new RefreshTokenRequest(repeatLoginResponse.RefreshToken), CancellationToken.None);
        AssertUnauthorizedError(refreshAfterLogout, "refresh_token_reuse_detected");

        var emptyRefresh = await controller.Refresh(null!, CancellationToken.None);
        AssertUnauthorizedError(emptyRefresh, "invalid_refresh_token");
    }

    [Fact]
    public async Task Refresh_Should_Reject_Session_Created_Before_Session_Version_Change()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var db = CreateSqliteDbContext(connection);
        await db.Database.EnsureCreatedAsync();
        var password = "CorrectPassword123!";
        db.Users.Add(new User
        {
            Id = Guid.NewGuid(),
            Email = "versioned-refresh@example.test",
            DisplayName = "Versioned Refresh",
            PasswordHash = new PasswordService().Hash(password),
            RolesCsv = UserRoles.User,
            Status = UserStatus.Active,
            ReferralCode = "versioned-refresh"
        });
        await db.SaveChangesAsync();
        var controller = CreateAuthController(db);
        var login = await controller.Login(new LoginRequest("versioned-refresh@example.test", password), CancellationToken.None);
        var response = Assert.IsType<AuthResponse>(Assert.IsType<OkObjectResult>(login).Value);
        var user = await db.Users.SingleAsync();
        user.SessionVersion++;
        await db.SaveChangesAsync();

        var refresh = await controller.Refresh(new RefreshTokenRequest(response.RefreshToken), CancellationToken.None);

        AssertUnauthorizedError(refresh, "session_invalidated");
        var session = await db.UserRefreshTokens.SingleAsync();
        Assert.NotNull(session.RevokedAt);
        Assert.Equal("session_version_mismatch", session.RevocationReason);
    }

    [Fact]
    public async Task Replayed_Logged_Out_Token_Should_Not_Revoke_Independent_Login()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var db = CreateSqliteDbContext(connection);
        await db.Database.EnsureCreatedAsync();
        var password = "CorrectPassword123!";
        db.Users.Add(new User
        {
            Id = Guid.NewGuid(),
            Email = "independent-session@example.test",
            DisplayName = "Independent Session",
            PasswordHash = new PasswordService().Hash(password),
            RolesCsv = UserRoles.User,
            Status = UserStatus.Active,
            ReferralCode = "independent-session"
        });
        await db.SaveChangesAsync();
        var controller = CreateAuthController(db);
        var firstLogin = Assert.IsType<AuthResponse>(Assert.IsType<OkObjectResult>(
            await controller.Login(new LoginRequest("independent-session@example.test", password), CancellationToken.None)).Value);
        var secondLogin = Assert.IsType<AuthResponse>(Assert.IsType<OkObjectResult>(
            await controller.Login(new LoginRequest("independent-session@example.test", password), CancellationToken.None)).Value);

        Assert.IsType<OkObjectResult>(await controller.Logout(new LogoutRequest(firstLogin.RefreshToken), CancellationToken.None));
        AssertUnauthorizedError(
            await controller.Refresh(new RefreshTokenRequest(firstLogin.RefreshToken), CancellationToken.None),
            "refresh_token_reuse_detected");

        var independentRefresh = await controller.Refresh(
            new RefreshTokenRequest(secondLogin.RefreshToken),
            CancellationToken.None);
        Assert.IsType<AuthResponse>(Assert.IsType<OkObjectResult>(independentRefresh).Value);
    }

    [Fact]
    public async Task Replayed_Legacy_Rotated_Token_Should_Revoke_Only_Its_Descendants()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var db = CreateSqliteDbContext(connection);
        await db.Database.EnsureCreatedAsync();
        var password = "CorrectPassword123!";
        db.Users.Add(new User
        {
            Id = Guid.NewGuid(),
            Email = "legacy-family@example.test",
            DisplayName = "Legacy Family",
            PasswordHash = new PasswordService().Hash(password),
            RolesCsv = UserRoles.User,
            Status = UserStatus.Active,
            ReferralCode = "legacy-family"
        });
        await db.SaveChangesAsync();
        var controller = CreateAuthController(db);
        var familyRoot = Assert.IsType<AuthResponse>(Assert.IsType<OkObjectResult>(
            await controller.Login(new LoginRequest("legacy-family@example.test", password), CancellationToken.None)).Value);
        var familyChild = Assert.IsType<AuthResponse>(Assert.IsType<OkObjectResult>(
            await controller.Refresh(new RefreshTokenRequest(familyRoot.RefreshToken), CancellationToken.None)).Value);
        var legacyIds = await db.UserRefreshTokens.Select(x => x.Id).ToListAsync();
        var independent = Assert.IsType<AuthResponse>(Assert.IsType<OkObjectResult>(
            await controller.Login(new LoginRequest("legacy-family@example.test", password), CancellationToken.None)).Value);
        var legacyRows = await db.UserRefreshTokens.Where(x => legacyIds.Contains(x.Id)).ToListAsync();
        foreach (var row in legacyRows)
        {
            row.FamilyId = null;
        }
        await db.SaveChangesAsync();

        AssertUnauthorizedError(
            await controller.Refresh(new RefreshTokenRequest(familyRoot.RefreshToken), CancellationToken.None),
            "refresh_token_reuse_detected");
        AssertUnauthorizedError(
            await controller.Refresh(new RefreshTokenRequest(familyChild.RefreshToken), CancellationToken.None),
            "refresh_token_reuse_detected");
        Assert.IsType<AuthResponse>(Assert.IsType<OkObjectResult>(
            await controller.Refresh(new RefreshTokenRequest(independent.RefreshToken), CancellationToken.None)).Value);
    }

    [Fact]
    public async Task Concurrent_Refresh_Should_Revoke_The_Winning_Family_And_Reject_The_Stale_Commit()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"vpn-refresh-race-{Guid.NewGuid():N}.db");
        try
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlite($"Data Source={databasePath}")
                .Options;
            var userId = Guid.NewGuid();
            var familyId = Guid.NewGuid();
            const string rawRefreshToken = "concurrent-refresh-token";
            await using (var seed = new ApplicationDbContext(options))
            {
                await seed.Database.EnsureCreatedAsync();
                seed.Users.Add(new User
                {
                    Id = userId,
                    Email = "concurrent-refresh@example.test",
                    DisplayName = "Concurrent Refresh",
                    PasswordHash = new PasswordService().Hash("CorrectPassword123!"),
                    RolesCsv = UserRoles.User,
                    Status = UserStatus.Active,
                    ReferralCode = "concurrent-refresh"
                });
                seed.UserRefreshTokens.Add(new UserRefreshToken
                {
                    UserId = userId,
                    SessionVersion = 0,
                    FamilyId = familyId,
                    TokenHash = HashToken(rawRefreshToken),
                    ExpiresAt = DateTimeOffset.UtcNow.AddDays(1)
                });
                await seed.SaveChangesAsync();
            }

            await using var db = new BeforeFirstSaveDbContext(options, async () =>
            {
                await using var competitor = new ApplicationDbContext(options);
                var competitorResult = await CreateAuthController(competitor).Refresh(
                    new RefreshTokenRequest(rawRefreshToken),
                    CancellationToken.None);
                Assert.IsType<AuthResponse>(Assert.IsType<OkObjectResult>(competitorResult).Value);
            });
            var action = await CreateAuthController(db).Refresh(
                new RefreshTokenRequest(rawRefreshToken),
                CancellationToken.None);

            AssertUnauthorizedError(action, "refresh_token_reuse_detected");
            await using var verify = new ApplicationDbContext(options);
            var sessions = await verify.UserRefreshTokens.AsNoTracking().ToListAsync();
            Assert.Equal(2, sessions.Count);
            Assert.All(sessions, session => Assert.NotNull(session.RevokedAt));
            Assert.Single(sessions, session => session.ReuseDetectedAt is not null);
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
    public async Task Logout_Should_Retry_And_Revoke_A_Child_Created_By_Concurrent_Refresh()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"vpn-logout-refresh-race-{Guid.NewGuid():N}.db");
        try
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlite($"Data Source={databasePath}")
                .Options;
            var userId = Guid.NewGuid();
            const string rawRefreshToken = "logout-refresh-race-token";
            await using (var seed = new ApplicationDbContext(options))
            {
                await seed.Database.EnsureCreatedAsync();
                seed.Users.Add(new User
                {
                    Id = userId,
                    Email = "logout-refresh-race@example.test",
                    DisplayName = "Logout Refresh Race",
                    PasswordHash = new PasswordService().Hash("CorrectPassword123!"),
                    RolesCsv = UserRoles.User,
                    Status = UserStatus.Active,
                    ReferralCode = "logout-refresh-race"
                });
                seed.UserRefreshTokens.Add(new UserRefreshToken
                {
                    UserId = userId,
                    FamilyId = Guid.NewGuid(),
                    TokenHash = HashToken(rawRefreshToken),
                    ExpiresAt = DateTimeOffset.UtcNow.AddDays(1)
                });
                await seed.SaveChangesAsync();
            }

            await using var db = new BeforeFirstSaveDbContext(options, async () =>
            {
                await using var competitor = new ApplicationDbContext(options);
                var competitorResult = await CreateAuthController(competitor).Refresh(
                    new RefreshTokenRequest(rawRefreshToken),
                    CancellationToken.None);
                Assert.IsType<AuthResponse>(Assert.IsType<OkObjectResult>(competitorResult).Value);
            });
            var action = await CreateAuthController(db).Logout(
                new LogoutRequest(rawRefreshToken),
                CancellationToken.None);

            Assert.IsType<OkObjectResult>(action);
            await using var verify = new ApplicationDbContext(options);
            var sessions = await verify.UserRefreshTokens.AsNoTracking().ToListAsync();
            Assert.Equal(2, sessions.Count);
            Assert.All(sessions, session => Assert.NotNull(session.RevokedAt));
            Assert.Single(sessions, session => session.RevocationReason == "rotated");
            Assert.Single(sessions, session => session.RevocationReason == "logout");
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
    public async Task Logout_Without_Refresh_Token_Should_Invalidate_All_User_Sessions()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var db = CreateSqliteDbContext(connection);
        await db.Database.EnsureCreatedAsync();
        var userId = Guid.NewGuid();
        db.Users.Add(new User
        {
            Id = userId,
            Email = "logout-all@example.test",
            DisplayName = "Logout All",
            PasswordHash = new PasswordService().Hash("CorrectPassword123!"),
            RolesCsv = UserRoles.User,
            Status = UserStatus.Active,
            ReferralCode = "logout-all"
        });
        db.UserRefreshTokens.AddRange(
            new UserRefreshToken
            {
                UserId = userId,
                FamilyId = Guid.NewGuid(),
                TokenHash = "logout-all-first",
                ExpiresAt = DateTimeOffset.UtcNow.AddDays(1)
            },
            new UserRefreshToken
            {
                UserId = userId,
                FamilyId = Guid.NewGuid(),
                TokenHash = "logout-all-second",
                ExpiresAt = DateTimeOffset.UtcNow.AddDays(1)
            });
        await db.SaveChangesAsync();
        var controller = CreateAuthController(db);
        controller.ControllerContext.HttpContext.User = Principal(userId.ToString());

        var action = await controller.Logout(null, CancellationToken.None);

        Assert.IsType<OkObjectResult>(action);
        Assert.Equal(1, (await db.Users.SingleAsync()).SessionVersion);
        var sessions = await db.UserRefreshTokens.ToListAsync();
        Assert.All(sessions, session => Assert.NotNull(session.RevokedAt));
        Assert.All(sessions, session => Assert.Equal("logout_all_current_user", session.RevocationReason));
        Assert.All(sessions, session => Assert.Equal(1, session.Revision));
        Assert.Single(await db.AuditLogs.Where(x => x.Action == "auth.logout_all").ToListAsync());
    }

    private static void AssertUnauthorizedError(IActionResult result, string expectedError)
    {
        var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result);
        var error = unauthorized.Value?.GetType().GetProperty("error")?.GetValue(unauthorized.Value)?.ToString();
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

    private static string HashToken(string token)
        => Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

    private static ClaimsPrincipal Principal(string subject, int sessionVersion = 0)
        => new(new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, subject),
                new Claim(AuthClaimTypes.SessionVersion, sessionVersion.ToString(System.Globalization.CultureInfo.InvariantCulture))
            ],
            "test"));

    private static string FindRepositoryRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "README.md"))
                && Directory.Exists(Path.Combine(current.FullName, "backend")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new InvalidOperationException("Repository root not found.");
    }

    private sealed class TestClock : IClock
    {
        public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
    }

    private sealed class BeforeFirstSaveDbContext(
        DbContextOptions<ApplicationDbContext> options,
        Func<Task> beforeFirstSave) : ApplicationDbContext(options)
    {
        private int _injected;

        public override async Task<int> SaveChangesAsync(
            bool acceptAllChangesOnSuccess,
            CancellationToken cancellationToken = default)
        {
            if (Interlocked.Exchange(ref _injected, 1) == 0)
            {
                await beforeFirstSave();
            }

            return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }
    }
}
