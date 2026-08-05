using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using VpnPlatform.Api.Contracts;
using VpnPlatform.Api.Controllers.Admin;
using VpnPlatform.Api.Controllers.Auth;
using VpnPlatform.Api.Controllers.Me;
using VpnPlatform.Application.Abstractions;
using VpnPlatform.Application.Common;
using VpnPlatform.Application.Services;
using VpnPlatform.Domain.Entities;
using VpnPlatform.Domain.Enums;
using VpnPlatform.Infrastructure.Auth;
using VpnPlatform.Infrastructure.Persistence;
using VpnPlatform.Infrastructure.Security;
using VpnPlatform.Infrastructure.Services;
using VpnPlatform.Infrastructure.Vpn;
using Xunit;

namespace VpnPlatform.UnitTests;

public class SecurityHardeningMvpTests
{
    [Fact]
    public void SecretProtector_Should_Protect_And_Unprotect_Without_Returning_Plaintext()
    {
        var protector = CreateSecretProtector();

        var protectedValue = protector.Protect("panel-password-secret");

        Assert.StartsWith("v1:", protectedValue, StringComparison.Ordinal);
        Assert.NotEqual("panel-password-secret", protectedValue);
        Assert.Equal("panel-password-secret", protector.Unprotect(protectedValue));
        Assert.DoesNotContain("panel-password-secret", protector.Mask(protectedValue), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void SensitiveDataRedactor_Should_Redact_Secrets_And_Protected_Payloads()
    {
        var text = "password=plain token:raw-token bot_token=telegram-secret webhook_secret=hook x3ui_password=panel Authorization: Bearer abc v1:YWJjZA==";

        var redacted = SensitiveDataRedactor.Redact(text);

        Assert.Contains("***REDACTED***", redacted, StringComparison.Ordinal);
        Assert.DoesNotContain("plain", redacted, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("raw-token", redacted, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("telegram-secret", redacted, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("webhook_secret=hook", redacted, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Bearer abc", redacted, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("YWJjZA", redacted, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Admin_AddServer_Should_Store_New_Panel_And_Ssh_Secrets_Protected()
    {
        await using var db = CreateDbContext();
        var protector = CreateSecretProtector();
        var controller = CreateOperationsController(db, protector);

        var result = await controller.AddServer(new CreateServerHttpRequest(
            Name: "Secured node",
            Host: "secured-node.example.test",
            IpAddress: "",
            Provider: "admin-vps",
            Region: "EU",
            Country: "NL",
            Datacenter: "AMS",
            Capacity: 100,
            SupportedProtocolsCsv: null,
            Priority: 100,
            TagsCsv: null,
            SshUser: "root",
            SshPort: 22,
            SshPrivateKeyPath: null,
            SkipHostKeyChecking: true,
            PanelBaseUrl: "https://panel.example.test",
            PanelUsername: "admin",
            PanelPassword: "panel-password-must-not-leak",
            PanelInboundId: null,
            PublicHostname: "secured-node.example.test",
            PublicPort: 443,
            NodeGroupId: null,
            SshAuthMethod: "password",
            SshCredential: "ssh-password-must-not-leak",
            ValidationMode: true,
            OwnerType: "admin"), CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
        var node = await db.VpnNodes.SingleAsync();
        Assert.Empty(node.PanelPassword);
        Assert.Empty(node.SshPrivateKeyPath);
        Assert.StartsWith("v1:", node.ProtectedPanelPassword, StringComparison.Ordinal);
        Assert.StartsWith("v1:", node.ProtectedSshCredential, StringComparison.Ordinal);
        Assert.NotEqual("panel-password-must-not-leak", node.ProtectedPanelPassword);
        Assert.NotEqual("ssh-password-must-not-leak", node.ProtectedSshCredential);

        var json = JsonSerializer.Serialize(Assert.IsType<OkObjectResult>(result).Value);
        Assert.Contains("PanelPasswordConfigured", json, StringComparison.Ordinal);
        Assert.Contains("SshCredentialConfigured", json, StringComparison.Ordinal);
        Assert.DoesNotContain("panel-password-must-not-leak", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ssh-password-must-not-leak", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("v1:", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("secretref:", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Admin_UpdateServer_Should_Edit_Metadata_And_Preserve_WriteOnly_Secrets()
    {
        await using var db = CreateDbContext();
        var protector = CreateSecretProtector();
        var controller = CreateOperationsController(db, protector);

        var create = Assert.IsType<OkObjectResult>(await controller.AddServer(new CreateServerHttpRequest(
            Name: "Edit node",
            Host: "edit-node.example.test",
            IpAddress: "",
            Provider: "admin-vps",
            Region: "EU",
            Country: "NL",
            Datacenter: "AMS",
            Capacity: 100,
            SupportedProtocolsCsv: "vless",
            Priority: 100,
            TagsCsv: "tier:standard",
            SshUser: "root",
            SshPort: 22,
            SshPrivateKeyPath: null,
            SkipHostKeyChecking: true,
            PanelBaseUrl: "https://panel.example.test",
            PanelUsername: "admin",
            PanelPassword: "initial-panel-secret",
            PanelInboundId: 1,
            PublicHostname: "edit-node.example.test",
            PublicPort: 443,
            NodeGroupId: null,
            SshAuthMethod: "password",
            SshCredential: "initial-ssh-secret",
            ValidationMode: true,
            OwnerType: "admin"), CancellationToken.None));

        var node = await db.VpnNodes.SingleAsync();
        var originalPanelSecret = node.ProtectedPanelPassword;
        var originalSshSecret = node.ProtectedSshCredential;

        var update = await controller.UpdateServer(node.Id, new CreateServerHttpRequest(
            Name: "Edited node",
            Host: "edited-node.example.test",
            IpAddress: "203.0.113.20",
            Provider: "hetzner",
            Region: "eu-west",
            Country: "DE",
            Datacenter: "fsn1",
            Capacity: 200,
            SupportedProtocolsCsv: "vless,vmess",
            Priority: 250,
            TagsCsv: "tier:premium,source:manual,validation-mode:false",
            SshUser: "ubuntu",
            SshPort: 2222,
            SshPrivateKeyPath: null,
            SkipHostKeyChecking: false,
            PanelBaseUrl: "https://edited-panel.example.test",
            PanelUsername: "root-admin",
            PanelPassword: "",
            PanelInboundId: 7,
            PublicHostname: "vpn-edited.example.test",
            PublicPort: 8443,
            NodeGroupId: null,
            SshAuthMethod: "password",
            SshCredential: "",
            ValidationMode: false,
            OwnerType: "ops"), CancellationToken.None);

        Assert.IsType<OkObjectResult>(update);
        Assert.Equal("Edited node", node.Name);
        Assert.Equal("fsn1", node.Datacenter);
        Assert.Equal(250, node.Priority);
        Assert.Equal("vless,vmess", node.SupportedProtocolsCsv);
        Assert.Equal("ubuntu", node.SshUser);
        Assert.Equal(2222, node.SshPort);
        Assert.Equal("https://edited-panel.example.test", node.PanelBaseUrl);
        Assert.Equal("root-admin", node.PanelUsername);
        Assert.Equal(7, node.PanelInboundId);
        Assert.Equal(originalPanelSecret, node.ProtectedPanelPassword);
        Assert.Equal(originalSshSecret, node.ProtectedSshCredential);
        Assert.Contains("tier:premium", node.TagsCsv, StringComparison.Ordinal);
        Assert.Contains("source:admin", node.TagsCsv, StringComparison.Ordinal);
        Assert.Contains("owner:ops", node.TagsCsv, StringComparison.Ordinal);
        Assert.Contains("validation-mode:false", node.TagsCsv, StringComparison.Ordinal);
        Assert.DoesNotContain("source:manual", node.TagsCsv, StringComparison.Ordinal);

        var json = JsonSerializer.Serialize(Assert.IsType<OkObjectResult>(update).Value);
        Assert.DoesNotContain("initial-panel-secret", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("initial-ssh-secret", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("v1:", json, StringComparison.OrdinalIgnoreCase);
        Assert.NotNull(create.Value);
    }

    [Fact]
    public async Task Admin_UpdateServer_Should_Rotate_Ssh_And_Panel_Secrets_With_Redacted_Audit()
    {
        await using var db = CreateDbContext();
        var protector = CreateSecretProtector();
        var controller = CreateOperationsController(db, protector);

        var create = Assert.IsType<OkObjectResult>(await controller.AddServer(new CreateServerHttpRequest(
            Name: "Rotate node",
            Host: "rotate-node.example.test",
            IpAddress: "",
            Provider: "admin-vps",
            Region: "EU",
            Country: "NL",
            Datacenter: "AMS",
            Capacity: 100,
            SupportedProtocolsCsv: "vless",
            Priority: 100,
            TagsCsv: "tier:standard",
            SshUser: "root",
            SshPort: 22,
            SshPrivateKeyPath: null,
            SkipHostKeyChecking: true,
            PanelBaseUrl: "https://panel.example.test",
            PanelUsername: "admin",
            PanelPassword: "old-panel-secret",
            PanelInboundId: 1,
            PublicHostname: "rotate-node.example.test",
            PublicPort: 443,
            NodeGroupId: null,
            SshAuthMethod: "ssh_key",
            SshCredential: "old-ssh-secret",
            ValidationMode: true,
            OwnerType: "admin"), CancellationToken.None));

        var node = await db.VpnNodes.SingleAsync();
        var originalSshSecret = node.ProtectedSshCredential;
        var originalSshRef = node.SshCredentialRef;
        var originalPanelSecret = node.ProtectedPanelPassword;
        var originalPanelRef = node.PanelSecretRef;

        var update = await controller.UpdateServer(node.Id, new CreateServerHttpRequest(
            Name: "Rotate node",
            Host: "rotate-node.example.test",
            IpAddress: "",
            Provider: "admin-vps",
            Region: "EU",
            Country: "NL",
            Datacenter: "AMS",
            Capacity: 100,
            SupportedProtocolsCsv: "vless",
            Priority: 100,
            TagsCsv: "tier:standard",
            SshUser: "root",
            SshPort: 22,
            SshPrivateKeyPath: null,
            SkipHostKeyChecking: true,
            PanelBaseUrl: "https://panel.example.test",
            PanelUsername: "admin",
            PanelPassword: "new-panel-secret-must-not-leak",
            PanelInboundId: 1,
            PublicHostname: "rotate-node.example.test",
            PublicPort: 443,
            NodeGroupId: null,
            SshAuthMethod: "ssh_key",
            SshCredential: "new-ssh-secret-must-not-leak",
            ValidationMode: true,
            OwnerType: "admin"), CancellationToken.None);

        Assert.IsType<OkObjectResult>(update);
        Assert.NotEqual(originalSshSecret, node.ProtectedSshCredential);
        Assert.NotEqual(originalSshRef, node.SshCredentialRef);
        Assert.NotEqual(originalPanelSecret, node.ProtectedPanelPassword);
        Assert.NotEqual(originalPanelRef, node.PanelSecretRef);
        Assert.Empty(node.SshPrivateKeyPath);
        Assert.Empty(node.PanelPassword);

        var rotateAudit = await db.AuditLogs.SingleAsync(x => x.Action == "server.secret.rotate");
        var auditJson = $"{rotateAudit.BeforeJson}\n{rotateAudit.AfterJson}";
        Assert.Contains("rotatedSshCredential", auditJson, StringComparison.Ordinal);
        Assert.Contains("rotatedPanelPassword", auditJson, StringComparison.Ordinal);
        Assert.DoesNotContain("old-ssh-secret", auditJson, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("new-ssh-secret-must-not-leak", auditJson, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("old-panel-secret", auditJson, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("new-panel-secret-must-not-leak", auditJson, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("secretref:", auditJson, StringComparison.OrdinalIgnoreCase);
        Assert.NotNull(create.Value);
    }

    [Fact]
    public async Task Auth_Login_Refresh_Logout_Should_Rotate_And_Revoke_Hashed_Refresh_Tokens()
    {
        await using var db = CreateDbContext();
        var controller = CreateAuthController(db, returnResetTokenForValidation: false);
        var password = "CorrectHorseBatteryStaple1";
        db.Users.Add(new User
        {
            Id = Guid.NewGuid(),
            Email = "auth@example.test",
            DisplayName = "Auth User",
            PasswordHash = new PasswordService().Hash(password),
            RolesCsv = UserRoles.User,
            Status = UserStatus.Active,
            ReferralCode = "AUTHREF"
        });
        await db.SaveChangesAsync();

        var login = await controller.Login(new LoginRequest("auth@example.test", password), CancellationToken.None);
        var loginResponse = Assert.IsType<AuthResponse>(Assert.IsType<OkObjectResult>(login).Value);
        Assert.False(string.IsNullOrWhiteSpace(loginResponse.RefreshToken));
        Assert.Single(await db.UserRefreshTokens.ToListAsync());
        Assert.DoesNotContain(loginResponse.RefreshToken, JsonSerializer.Serialize(await db.UserRefreshTokens.ToListAsync()), StringComparison.Ordinal);

        var refresh = await controller.Refresh(new RefreshTokenRequest(loginResponse.RefreshToken), CancellationToken.None);
        var refreshed = Assert.IsType<AuthResponse>(Assert.IsType<OkObjectResult>(refresh).Value);
        Assert.NotEqual(loginResponse.RefreshToken, refreshed.RefreshToken);
        Assert.Equal(2, await db.UserRefreshTokens.CountAsync());
        Assert.Single(await db.UserRefreshTokens.Where(x => x.RevokedAt == null).ToListAsync());

        var oldTokenReuse = await controller.Refresh(new RefreshTokenRequest(loginResponse.RefreshToken), CancellationToken.None);
        Assert.IsType<UnauthorizedObjectResult>(oldTokenReuse);
        Assert.Empty(await db.UserRefreshTokens.Where(x => x.RevokedAt == null).ToListAsync());

        var secondLogin = Assert.IsType<AuthResponse>(Assert.IsType<OkObjectResult>(await controller.Login(new LoginRequest("auth@example.test", password), CancellationToken.None)).Value);
        var logout = await controller.Logout(new LogoutRequest(secondLogin.RefreshToken), CancellationToken.None);
        Assert.IsType<OkObjectResult>(logout);
        Assert.Empty(await db.UserRefreshTokens.Where(x => x.RevokedAt == null).ToListAsync());
    }

    [Fact]
    public async Task Password_Reset_Should_Store_Hashed_One_Time_Token_And_Change_Password()
    {
        await using var db = CreateDbContext();
        var passwordService = new PasswordService();
        db.Users.Add(new User
        {
            Id = Guid.NewGuid(),
            Email = "reset@example.test",
            DisplayName = "Reset User",
            PasswordHash = passwordService.Hash("OldPassword123!"),
            RolesCsv = UserRoles.User,
            Status = UserStatus.Active,
            ReferralCode = "RESETREF"
        });
        await db.SaveChangesAsync();
        var controller = CreateAuthController(db, returnResetTokenForValidation: true);

        var forgot = await controller.ForgotPassword(new ForgotPasswordRequest("reset@example.test"), CancellationToken.None);
        var forgotResponse = Assert.IsType<ForgotPasswordResponse>(Assert.IsType<OkObjectResult>(forgot).Value);
        Assert.True(forgotResponse.Accepted);
        Assert.False(string.IsNullOrWhiteSpace(forgotResponse.ValidationResetToken));
        var stored = await db.PasswordResetTokens.SingleAsync();
        Assert.DoesNotContain(forgotResponse.ValidationResetToken!, JsonSerializer.Serialize(stored), StringComparison.Ordinal);

        var reset = await controller.ResetPassword(new ResetPasswordRequest(forgotResponse.ValidationResetToken!, "NewPassword123!"), CancellationToken.None);
        Assert.IsType<OkObjectResult>(reset);
        stored = await db.PasswordResetTokens.SingleAsync();
        Assert.NotNull(stored.UsedAt);
        var user = await db.Users.SingleAsync();
        Assert.True(passwordService.Verify("NewPassword123!", user.PasswordHash));

        var reuse = await controller.ResetPassword(new ResetPasswordRequest(forgotResponse.ValidationResetToken!, "AnotherPassword123!"), CancellationToken.None);
        Assert.IsType<BadRequestObjectResult>(reuse);
    }

    [Fact]
    public void QrCodeGenerator_Should_Return_Svg_Containing_Encoded_Vpn_Uri()
    {
        var generator = new SvgQrCodeGenerator(new TestClock());
        var uri = "vless://user@example.test:443?security=tls#vpn-platform";

        var qr = generator.GenerateSvg(uri, "test");

        Assert.Equal("image/svg+xml", qr.MediaType);
        Assert.Contains("<svg", qr.Content, StringComparison.OrdinalIgnoreCase);
        Assert.NotEqual(uri, qr.Content);
    }

    [Fact]
    public async Task Cabinet_Qr_Endpoint_Should_Return_Own_Access_And_Block_Other_Users()
    {
        await using var db = CreateDbContext();
        var ownerId = Guid.NewGuid();
        var otherId = Guid.NewGuid();
        var ownerSubscriptionId = Guid.NewGuid();
        var otherSubscriptionId = Guid.NewGuid();
        var ownAccessId = Guid.NewGuid();
        var otherAccessId = Guid.NewGuid();
        db.Subscriptions.AddRange(
            new Subscription { Id = ownerSubscriptionId, UserId = ownerId, TariffId = Guid.NewGuid(), Status = SubscriptionStatus.Active, StartAt = DateTimeOffset.UtcNow.AddDays(-1), EndAt = DateTimeOffset.UtcNow.AddDays(10) },
            new Subscription { Id = otherSubscriptionId, UserId = otherId, TariffId = Guid.NewGuid(), Status = SubscriptionStatus.Active, StartAt = DateTimeOffset.UtcNow.AddDays(-1), EndAt = DateTimeOffset.UtcNow.AddDays(10) });
        db.AccessCredentials.AddRange(
            new AccessCredential { Id = ownAccessId, SubscriptionId = ownerSubscriptionId, AccessUri = "vless://owner@example.test", Status = AccessCredentialStatus.Active },
            new AccessCredential { Id = otherAccessId, SubscriptionId = otherSubscriptionId, AccessUri = "vless://other@example.test", Status = AccessCredentialStatus.Active });
        await db.SaveChangesAsync();

        var controller = new CabinetAccessController(db, new SvgQrCodeGenerator(new TestClock()));
        controller.ControllerContext = new ControllerContext { HttpContext = HttpContextForUser(ownerId) };

        var ownQr = await controller.GetAccessQr(ownAccessId, CancellationToken.None);
        var forbidden = await controller.GetAccessQr(otherAccessId, CancellationToken.None);

        var content = Assert.IsType<ContentResult>(ownQr);
        Assert.Equal("image/svg+xml", content.ContentType);
        Assert.Contains("<svg", content.Content, StringComparison.OrdinalIgnoreCase);
        Assert.IsType<NotFoundObjectResult>(forbidden);
    }

    private static AuthController CreateAuthController(ApplicationDbContext db, bool returnResetTokenForValidation)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Issuer"] = "vpn-platform-test",
                ["Jwt:Audience"] = "vpn-platform-test",
                ["Jwt:SigningKey"] = "unit-test-jwt-signing-key-0000000000000000000000",
                ["Security:SecretEncryptionKey"] = "unit-test-secret-encryption-key-000000000000000000",
                ["Auth:RefreshTokenDays"] = "30",
                ["Auth:PasswordReset:ExpiryMinutes"] = "30",
                ["Auth:PasswordReset:ReturnTokenForValidation"] = returnResetTokenForValidation ? "true" : "false"
            })
            .Build();
        var controller = new AuthController(db, new PasswordService(), new JwtTokenService(configuration), new TestClock(), configuration, new SecretProtector(configuration));
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
        return controller;
    }

    private static AdminOperationsController CreateOperationsController(ApplicationDbContext db, ISecretProtector protector)
    {
        var provisioning = new ProvisioningService(db, new TestClock(), protector);
        var controller = new AdminOperationsController(
            db,
            provisioning,
            paymentOrchestrator: null!,
            paymentProviderAccounts: new PaymentProviderAccountService(db, protector, new TestClock()),
            vpnAccessLifecycleService: null,
            secretProtector: protector,
            qrCodeGenerator: new SvgQrCodeGenerator(new TestClock()));
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
        return controller;
    }

    private static ISecretProtector CreateSecretProtector()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Security:SecretEncryptionKey"] = "unit-test-secret-encryption-key-0000000000000000000000"
            })
            .Build();
        return new SecretProtector(configuration, new TestHostEnvironment("Production"));
    }

    private static DefaultHttpContext HttpContextForUser(Guid userId)
    {
        var context = new DefaultHttpContext();
        context.User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) }, "test"));
        return context;
    }

    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new ApplicationDbContext(options);
    }

    private sealed class TestClock : IClock
    {
        public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
    }

    private sealed class TestHostEnvironment : IHostEnvironment
    {
        public TestHostEnvironment(string environmentName)
        {
            EnvironmentName = environmentName;
        }

        public string EnvironmentName { get; set; }
        public string ApplicationName { get; set; } = "VpnPlatform.UnitTests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } = new Microsoft.Extensions.FileProviders.NullFileProvider();
    }
}
