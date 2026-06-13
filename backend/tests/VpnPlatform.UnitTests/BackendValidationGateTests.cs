using Xunit;

namespace VpnPlatform.UnitTests;

public class BackendValidationGateTests
{
    [Fact]
    public void Backend_Validation_Gates_Should_Run_Full_Restore_Build_Test_And_Ef_Checks()
    {
        var root = FindRepositoryRoot();
        var bashGate = File.ReadAllText(Path.Combine(root, "scripts", "validate-backend.sh"));
        var powershellGate = File.ReadAllText(Path.Combine(root, "scripts", "validate-backend.ps1"));

        foreach (var gate in new[] { bashGate, powershellGate })
        {
            Assert.Contains("dotnet restore", gate, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("backend/VpnPlatform.sln", gate.Replace('\\', '/'), StringComparison.OrdinalIgnoreCase);
            Assert.Contains("dotnet build", gate, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("dotnet test", gate, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("--no-build", gate, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("test-results.trx", gate, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("dotnet tool restore", gate, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("dotnet ef migrations list", gate, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("--no-connect", gate, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("check-ef-drift", gate, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void Backend_Validation_Gates_Should_Use_Safe_Defaults_And_Secret_Scan()
    {
        var root = FindRepositoryRoot();
        var bashGate = File.ReadAllText(Path.Combine(root, "scripts", "validate-backend.sh"));
        var powershellGate = File.ReadAllText(Path.Combine(root, "scripts", "validate-backend.ps1"));

        foreach (var gate in new[] { bashGate, powershellGate })
        {
            Assert.Contains("scan-secrets", gate, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Database__ApplyMigrationsOnStartup", gate, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Database__SeedDemoData", gate, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("AdminBootstrap__Enabled", gate, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Provisioning__LiveExecutionEnabled", gate, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Provisioning__AllowLiveDeploy", gate, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("TelegramBot__Enabled", gate, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Payments__YooKassa__Mode", gate, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Payments__Stripe__Mode", gate, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Vpn__X3Ui__Mode", gate, StringComparison.OrdinalIgnoreCase);
        }

        Assert.Contains("check-validation-safety.sh", bashGate, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("check-validation-safety.sh", powershellGate, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Get-BashCommand", powershellGate, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Backend_Validation_Documentation_And_Roadmap_Should_Expose_Current_Green_Count()
    {
        var root = FindRepositoryRoot();
        var docs = File.ReadAllText(Path.Combine(root, "docs", "backend-validation-gate.md"));
        var roadmap = File.ReadAllText(Path.Combine(root, "docs", "PRODUCT_COMPLETION_ROADMAP.md"));
        var testResults = File.ReadAllText(Path.Combine(root, "TEST_RESULTS.md"));

        Assert.Contains("433/433", docs, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("[x] `P9-TST-001`", roadmap, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("433/433", roadmap, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("2026-06-13-backend-validation-gate", testResults, StringComparison.OrdinalIgnoreCase);
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, ".env.example")) && Directory.Exists(Path.Combine(directory.FullName, "backend")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Repository root was not found for backend validation gate tests.");
    }
}
