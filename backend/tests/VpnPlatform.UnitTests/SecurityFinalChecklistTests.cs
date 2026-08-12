using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VpnPlatform.Api.Controllers.Admin;
using VpnPlatform.Api.Middleware;
using VpnPlatform.Api.Security;
using VpnPlatform.Application.Common;
using Xunit;

namespace VpnPlatform.UnitTests;

public class SecurityFinalChecklistTests
{
    [Fact]
    public void Security_Final_Checklist_Should_Link_Guards_And_Current_Evidence()
    {
        var root = FindRepositoryRoot();
        var checklist = File.ReadAllText(Path.Combine(root, "docs", "security-final-checklist.md"));
        var roadmap = File.ReadAllText(Path.Combine(root, "docs", "PRODUCT_COMPLETION_ROADMAP.md"));
        var docsIndex = File.ReadAllText(Path.Combine(root, "docs", "README.md"));
        var testResults = File.ReadAllText(Path.Combine(root, "TEST_RESULTS.md"));

        Assert.Contains("P11-ACC-005", checklist, StringComparison.Ordinal);
        Assert.Contains("SecurityFinalChecklistTests", checklist, StringComparison.Ordinal);
        Assert.Contains("SecretScanTests", checklist, StringComparison.Ordinal);
        Assert.Contains("SecurityHardeningMvpTests", checklist, StringComparison.Ordinal);
        Assert.Contains("AdminAuthorizationPolicyTests", checklist, StringComparison.Ordinal);
        Assert.Contains("RateLimitingSecurityTests", checklist, StringComparison.Ordinal);
        Assert.Contains("SecurityHeadersTests", checklist, StringComparison.Ordinal);
        Assert.Contains("GitHubSecretsAuditTests", checklist, StringComparison.Ordinal);
        Assert.Contains("ProvisioningSecretMaterializerTests", checklist, StringComparison.Ordinal);
        Assert.Contains("PaymentWebhookIdempotencyContractTests", checklist, StringComparison.Ordinal);
        Assert.Contains("оператор обязан ротировать", checklist, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("1196/1196", checklist, StringComparison.Ordinal);
        Assert.Contains("2026-08-12-provisioning-credential-preflight", checklist, StringComparison.OrdinalIgnoreCase);

        Assert.Contains("[x] `P11-ACC-005`", roadmap, StringComparison.Ordinal);
        Assert.Contains("security-final-checklist.md", docsIndex, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("2026-06-14-security-final-checklist", testResults, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("SecurityFinalChecklistTests", testResults, StringComparison.Ordinal);
    }

    [Fact]
    public void Admin_Surface_Should_Keep_Policy_Authorization_And_No_Anonymous_Routes()
    {
        var controllerTypes = typeof(AdminOperationsController).Assembly.GetTypes()
            .Where(type => type.Namespace == "VpnPlatform.Api.Controllers.Admin")
            .Where(type => typeof(ControllerBase).IsAssignableFrom(type))
            .Where(type => !type.IsAbstract)
            .OrderBy(type => type.Name)
            .ToArray();

        Assert.Equal(new[]
        {
            nameof(AdminDashboardController),
            nameof(AdminFaqController),
            nameof(AdminOperationsController),
            nameof(AdminSessionController),
            nameof(AdminSiteContentController),
            nameof(AdminTelegramBotSettingsController),
            nameof(AdminUsersController),
            nameof(AdminVpnPanelsController),
            nameof(AdminWorkScenariosController)
        }, controllerTypes.Select(type => type.Name).ToArray());

        foreach (var controllerType in controllerTypes)
        {
            Assert.Empty(controllerType.GetCustomAttributes<AllowAnonymousAttribute>());

            var controllerPolicies = ReadAuthorizePolicies(controllerType.GetCustomAttributes<AuthorizeAttribute>());
            Assert.NotEmpty(controllerPolicies);
            Assert.All(controllerPolicies, AssertKnownAdminPolicy);

            foreach (var method in controllerType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
                         .Where(IsActionEndpoint))
            {
                Assert.Empty(method.GetCustomAttributes<AllowAnonymousAttribute>());

                var methodPolicies = ReadAuthorizePolicies(method.GetCustomAttributes<AuthorizeAttribute>()).ToArray();
                Assert.All(methodPolicies, AssertKnownAdminPolicy);

                if (!IsWriteEndpoint(method))
                {
                    continue;
                }

                var effectivePolicies = methodPolicies.Length > 0 ? methodPolicies : controllerPolicies;
                Assert.Contains(effectivePolicies, policy => !ReadOnlyPolicies.Contains(policy));
            }
        }
    }

    [Fact]
    public void Security_Final_Checklist_Should_Reference_Concrete_Runtime_Gates()
    {
        var root = FindRepositoryRoot();
        var program = File.ReadAllText(Path.Combine(root, "backend", "src", "VpnPlatform.Api", "Program.cs"));
        var nginx = File.ReadAllText(Path.Combine(root, "frontend", "nginx.security.conf"));
        var validateAll = File.ReadAllText(Path.Combine(root, "scripts", "validate-all.sh"));
        var validateBackendPowerShell = File.ReadAllText(Path.Combine(root, "scripts", "validate-backend.ps1"));
        var postDeploySmoke = File.ReadAllText(Path.Combine(root, "scripts", "post-deploy-smoke.sh"));

        Assert.Contains("scan-secrets.sh", validateAll, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("scan-secrets.ps1", validateBackendPowerShell, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("app.UseMiddleware<SecurityHeadersMiddleware>();", program, StringComparison.Ordinal);
        Assert.Contains("app.UseRateLimiter()", program, StringComparison.Ordinal);
        Assert.Contains("app.UseAuthentication()", program, StringComparison.Ordinal);
        Assert.Contains("app.UseAuthorization()", program, StringComparison.Ordinal);
        Assert.True(program.IndexOf("app.UseRateLimiter()", StringComparison.Ordinal) < program.IndexOf("app.UseAuthentication()", StringComparison.Ordinal));
        Assert.True(program.IndexOf("app.UseAuthentication()", StringComparison.Ordinal) < program.IndexOf("app.UseAuthorization()", StringComparison.Ordinal));
        Assert.Contains(nameof(SecurityHeadersMiddleware), typeof(SecurityHeadersMiddleware).FullName, StringComparison.Ordinal);
        Assert.Contains(ApiRateLimitPolicies.AuthSensitive, ApiRateLimitPolicies.Defaults.Keys);
        Assert.Contains(ApiRateLimitPolicies.PublicCheckout, ApiRateLimitPolicies.Defaults.Keys);
        Assert.Contains(ApiRateLimitPolicies.Webhook, ApiRateLimitPolicies.Defaults.Keys);
        Assert.Contains("Content-Security-Policy", nginx, StringComparison.Ordinal);
        Assert.Contains("Strict-Transport-Security", nginx, StringComparison.Ordinal);
        Assert.Contains("ADMIN_WEB_URL", postDeploySmoke, StringComparison.Ordinal);
        Assert.Contains("fetch_with_retry", postDeploySmoke, StringComparison.Ordinal);
    }

    private static readonly HashSet<string> ReadOnlyPolicies =
    [
        AdminPolicies.AdminRead,
        AdminPolicies.FinanceRead,
        AdminPolicies.SupportRead
    ];

    private static string[] ReadAuthorizePolicies(IEnumerable<AuthorizeAttribute> attributes)
        => attributes
            .Select(attribute => attribute.Policy)
            .Where(policy => !string.IsNullOrWhiteSpace(policy))
            .Select(policy => policy!)
            .Distinct(StringComparer.Ordinal)
            .ToArray();

    private static void AssertKnownAdminPolicy(string policy)
        => Assert.Contains(policy, AdminPolicies.PolicyRoles.Keys);

    private static bool IsActionEndpoint(MethodInfo method)
        => method.GetCustomAttributes().Any(attribute =>
            attribute is HttpGetAttribute
            || attribute is HttpPostAttribute
            || attribute is HttpPutAttribute
            || attribute is HttpPatchAttribute
            || attribute is HttpDeleteAttribute);

    private static bool IsWriteEndpoint(MethodInfo method)
        => method.GetCustomAttributes().Any(attribute =>
            attribute is HttpPostAttribute
            || attribute is HttpPutAttribute
            || attribute is HttpPatchAttribute
            || attribute is HttpDeleteAttribute);

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "README.md")) && Directory.Exists(Path.Combine(directory.FullName, "backend")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Repository root was not found for security final checklist tests.");
    }
}
