using System.Text.Json;
using Xunit;

namespace VpnPlatform.UnitTests;

public class PaymentProviderSmokeReportTests
{
    private static readonly string[] RequiredProviders =
    [
        "YooKassa",
        "RoboKassa",
        "YooMoney",
        "CloudPayments",
        "TBankAcquiring",
        "Prodamus",
        "Stripe",
        "PayPal"
    ];

    [Fact]
    public void Smoke_Report_Template_Should_List_All_Web_Providers_Without_TelegramStars()
    {
        var root = FindRepositoryRoot();
        using var json = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, "docs", "payment-provider-smoke-report.template.json")));

        var providers = json.RootElement.GetProperty("providers")
            .EnumerateArray()
            .Select(x => x.GetProperty("provider").GetString())
            .ToArray();

        Assert.Equal(RequiredProviders.Order(StringComparer.Ordinal), providers.Order(StringComparer.Ordinal));
        Assert.DoesNotContain("TelegramStars", providers);
    }

    [Fact]
    public void Smoke_Report_Template_Should_Be_Fail_Closed_Until_Real_Provider_Checks()
    {
        var root = FindRepositoryRoot();
        using var json = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, "docs", "payment-provider-smoke-report.template.json")));

        foreach (var provider in json.RootElement.GetProperty("providers").EnumerateArray())
        {
            Assert.Equal("sandbox", provider.GetProperty("mode").GetString());
            Assert.Equal("blocked", provider.GetProperty("status").GetString());
            Assert.False(provider.GetProperty("accountConfigured").GetBoolean());
            Assert.False(provider.GetProperty("checkoutCreated").GetBoolean());
            Assert.False(provider.GetProperty("providerConfirmation").GetBoolean());
            Assert.False(provider.GetProperty("webhookProcessed").GetBoolean());
            Assert.False(provider.GetProperty("subscriptionActivated").GetBoolean());
            Assert.False(provider.GetProperty("refundChecked").GetBoolean());
            Assert.Contains("without secrets", provider.GetProperty("evidence").GetString(), StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void Smoke_Report_Validator_Should_Check_All_Required_Provider_Gates()
    {
        var root = FindRepositoryRoot();
        var script = File.ReadAllText(Path.Combine(root, "scripts", "validate-payment-provider-smoke-report.ps1"));

        foreach (var provider in RequiredProviders)
        {
            Assert.Contains(provider, script, StringComparison.Ordinal);
        }

        foreach (var requiredField in new[]
                 {
                     "accountConfigured",
                     "checkoutCreated",
                     "providerConfirmation",
                     "webhookProcessed",
                     "subscriptionActivated",
                     "refundChecked",
                     "evidence",
                     "RequireAllPassed"
                 })
        {
            Assert.Contains(requiredField, script, StringComparison.OrdinalIgnoreCase);
        }

        Assert.Contains("Payment provider smoke report contains forbidden secret marker", script, StringComparison.Ordinal);
        Assert.Contains("must be passed when -RequireAllPassed is used", script, StringComparison.Ordinal);
    }

    [Fact]
    public void Smoke_Report_Validator_Should_Require_All_Provider_Boolean_Gates_For_Acceptance()
    {
        var root = FindRepositoryRoot();
        var script = File.ReadAllText(Path.Combine(root, "scripts", "validate-payment-provider-smoke-report.ps1"));
        var roadmap = File.ReadAllText(Path.Combine(root, "docs", "PRODUCT_COMPLETION_ROADMAP.md"));
        var guide = File.ReadAllText(Path.Combine(root, "docs", "payment-provider-smoke.md"));

        foreach (var requiredField in new[]
                 {
                     "accountConfigured",
                     "checkoutCreated",
                     "providerConfirmation",
                     "webhookProcessed",
                     "subscriptionActivated",
                     "refundChecked"
                 })
        {
            Assert.Contains(requiredField, script, StringComparison.OrdinalIgnoreCase);
            Assert.Contains(requiredField, guide, StringComparison.OrdinalIgnoreCase);
        }

        Assert.Contains("foreach ($booleanName in $requiredBooleans)", script, StringComparison.Ordinal);
        Assert.Contains("field $booleanName must be true when -RequireAllPassed is used", script, StringComparison.Ordinal);
        Assert.Contains("[x] `P0-PAY-014`", roadmap, StringComparison.Ordinal);
    }

    [Fact]
    public void Smoke_Report_Validator_Should_Reject_Stale_Release_In_Acceptance_Mode()
    {
        var root = FindRepositoryRoot();
        var script = File.ReadAllText(Path.Combine(root, "scripts", "validate-payment-provider-smoke-report.ps1"));
        var regression = File.ReadAllText(Path.Combine(root, "scripts", "test-payment-provider-smoke-report-latest-release-guard.ps1"));
        var roadmap = File.ReadAllText(Path.Combine(root, "docs", "PRODUCT_COMPLETION_ROADMAP.md"));
        var guide = File.ReadAllText(Path.Combine(root, "docs", "payment-provider-smoke.md"));

        foreach (var expected in new[]
                 {
                     "Get-LatestActiveReleaseId",
                     "AppReleases/releases.json",
                     "must match latest active release",
                     "-RequireAllPassed"
                 })
        {
            Assert.Contains(expected, script, StringComparison.OrdinalIgnoreCase);
        }

        Assert.Contains("stale-release-id", regression, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("must match latest active release", regression, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("latest active release", guide, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("[x] `P0-PAY-015`", roadmap, StringComparison.Ordinal);
    }

    [Fact]
    public void Smoke_Report_Generator_Should_Create_Safe_Blocked_Report()
    {
        var root = FindRepositoryRoot();
        var script = File.ReadAllText(Path.Combine(root, "scripts", "new-payment-provider-smoke-report.ps1"));
        var guide = File.ReadAllText(Path.Combine(root, "docs", "payment-provider-smoke.md"));

        foreach (var expected in new[]
                 {
                     "payment-provider-smoke-report.template.json",
                     "validate-payment-provider-smoke-report.ps1",
                     "ConvertTo-Json -Depth 8",
                     "Set-Content",
                     "-Encoding UTF8",
                     "blocked",
                     "TODO: run $Mode smoke",
                     "Output file already exists. Pass -Force",
                     "Get-LatestReleaseId",
                     "Assert-KnownReleaseId",
                     "DateTimeOffset]::Parse",
                     "CultureInfo]::InvariantCulture",
                     "ReleaseId must exist in backend/src/VpnPlatform.Api/AppReleases/releases.json"
                 })
        {
            Assert.Contains(expected, script, StringComparison.OrdinalIgnoreCase);
        }

        foreach (var field in new[] { "OutputPath", "EnvironmentName", "Operator", "ReleaseId", "Mode" })
        {
            Assert.Contains(field, script, StringComparison.Ordinal);
        }

        Assert.Contains("new-payment-provider-smoke-report.ps1", guide, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("test-payment-provider-smoke-report-generator-release-guard.ps1", guide, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("-Mode sandbox", guide, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("password=", script, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Bearer ", script, StringComparison.Ordinal);
        Assert.DoesNotContain("BEGIN OPENSSH PRIVATE KEY", script, StringComparison.Ordinal);
    }

    [Fact]
    public void Smoke_Report_Generator_Should_Reject_Unknown_Manual_ReleaseId()
    {
        var root = FindRepositoryRoot();
        var script = File.ReadAllText(Path.Combine(root, "scripts", "new-payment-provider-smoke-report.ps1"));
        var regression = File.ReadAllText(Path.Combine(root, "scripts", "test-payment-provider-smoke-report-generator-release-guard.ps1"));
        var roadmap = File.ReadAllText(Path.Combine(root, "docs", "PRODUCT_COMPLETION_ROADMAP.md"));

        foreach (var expected in new[]
                 {
                     "Assert-KnownReleaseId",
                     "AppReleases/releases.json",
                     "ReleaseId must exist in backend/src/VpnPlatform.Api/AppReleases/releases.json"
                 })
        {
            Assert.Contains(expected, script, StringComparison.OrdinalIgnoreCase);
        }

        Assert.Contains("missing-release-id-for-regression", regression, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Generator accepted unknown ReleaseId", regression, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Generator created report artifact after unknown ReleaseId failure", regression, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("payment provider smoke generator release guard valid", regression, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("[x] `P0-PAY-016`", roadmap, StringComparison.Ordinal);
    }

    [Fact]
    public void Documentation_Should_Link_Provider_Smoke_Report_To_Roadmap_And_Docs_Index()
    {
        var root = FindRepositoryRoot();
        var docsIndex = File.ReadAllText(Path.Combine(root, "docs", "README.md"));
        var paymentSmoke = File.ReadAllText(Path.Combine(root, "docs", "payment-provider-smoke.md"));
        var roadmap = File.ReadAllText(Path.Combine(root, "docs", "PRODUCT_COMPLETION_ROADMAP.md"));

        Assert.Contains("payment-provider-smoke.md", docsIndex, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("payment-provider-smoke-report.template.json", paymentSmoke, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("validate-payment-provider-smoke-report.ps1", paymentSmoke, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("[x] `P0-PAY-012`", roadmap, StringComparison.Ordinal);
        Assert.Contains("payment-provider-smoke-report.template.json", roadmap, StringComparison.OrdinalIgnoreCase);
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "README.md"))
                && File.Exists(Path.Combine(directory.FullName, "CHANGELOG.md"))
                && Directory.Exists(Path.Combine(directory.FullName, "backend")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Repository root was not found for payment provider smoke report tests.");
    }
}
