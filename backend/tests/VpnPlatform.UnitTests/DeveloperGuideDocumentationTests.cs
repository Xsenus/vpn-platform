using Xunit;

namespace VpnPlatform.UnitTests;

public class DeveloperGuideDocumentationTests
{
    [Fact]
    public void Developer_Guide_Should_Cover_Architecture_And_Extension_Workflows()
    {
        var guide = File.ReadAllText(Path.Combine(FindRepositoryRoot(), "docs", "developer-guide.md"));

        foreach (var required in new[]
        {
            "P10-DOC-004",
            "Domain -> Application -> Infrastructure/API",
            "User",
            "Tariff",
            "Order",
            "PaymentAttempt",
            "Subscription",
            "AccessCredential",
            "AppRelease",
            "ProvisioningRun",
            "StatusStateMachine",
            "PaymentOrchestrator",
            "IPaymentProvider",
            "IPaymentWebhookVerifier",
            "IPaymentStatusMapper",
            "PaymentProviderConfigurationRules",
            "VLESS",
            "VMess",
            "Trojan",
            "Provisioning__LiveExecutionEnabled=true",
            "dotnet test backend\\VpnPlatform.sln --configuration Release",
            "npm test --prefix frontend",
            "git diff --check"
        })
        {
            Assert.Contains(required, guide, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void Documentation_Index_Should_Link_Core_Guides()
    {
        var index = File.ReadAllText(Path.Combine(FindRepositoryRoot(), "docs", "README.md"));

        foreach (var required in new[]
        {
            "../README.md",
            "PRODUCT_COMPLETION_ROADMAP.md",
            "admin-guide.md",
            "user-guide.md",
            "developer-guide.md",
            "payment-providers.md",
            "provisioning.md",
            "rbac-policy-matrix.md",
            "playwright-public-e2e.md"
        })
        {
            Assert.Contains(required, index, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void Developer_Docs_Should_Not_Contain_Mojibake_Or_Replacement_Characters()
    {
        var root = FindRepositoryRoot();
        var files = new[]
        {
            Path.Combine(root, "docs", "developer-guide.md"),
            Path.Combine(root, "docs", "README.md"),
            Path.Combine(root, "backend", "tests", "VpnPlatform.UnitTests", "DeveloperGuideDocumentationTests.cs")
        };
        var forbidden = new[]
        {
            "\uFFFD",
            new string([('\u0420'), ('\u040E')]),
            new string([('\u0420'), ('\u045F')]),
            new string([('\u0420'), ('\u0491')]),
            new string([('\u0421'), ('\u0403')]),
            new string([('\u0420'), ('\u00B5'), ('\u0420')])
        };

        foreach (var file in files)
        {
            var text = File.ReadAllText(file);
            foreach (var marker in forbidden)
            {
                Assert.DoesNotContain(marker, text, StringComparison.Ordinal);
            }
        }
    }

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

        throw new InvalidOperationException("Repository root was not found for developer guide documentation tests.");
    }
}
