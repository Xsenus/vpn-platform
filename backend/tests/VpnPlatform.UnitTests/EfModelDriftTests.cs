using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using VpnPlatform.Infrastructure.Persistence;
using Xunit;

namespace VpnPlatform.UnitTests;

public class EfModelDriftTests
{
    [Fact]
    public void ApplicationDbContext_Model_Should_Match_Last_Migration_Snapshot()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql("Host=localhost;Database=vpn_platform_drift_check;Username=postgres;Password=postgres")
            .Options;

        using var db = new ApplicationDbContext(options);
        var migrationsAssembly = db.GetService<IMigrationsAssembly>();
        var snapshotModel = migrationsAssembly.ModelSnapshot?.Model;
        Assert.NotNull(snapshotModel);

        var runtimeInitializer = db.GetService<IModelRuntimeInitializer>();
        var finalizedSnapshot = runtimeInitializer.Initialize(snapshotModel);
        var currentDesignModel = db.GetService<IDesignTimeModel>().Model;
        var modelDiffer = db.GetService<IMigrationsModelDiffer>();

        var differences = modelDiffer
            .GetDifferences(finalizedSnapshot.GetRelationalModel(), currentDesignModel.GetRelationalModel())
            .ToList();

        Assert.True(
            differences.Count == 0,
            "EF model differs from the latest migration snapshot. Add a migration or update the snapshot. Differences: "
            + string.Join(", ", differences.Select(x => x.GetType().Name)));
    }

    [Fact]
    public void Ef_Drift_Check_Should_Have_Cross_Platform_Safe_Scripts_And_Documentation()
    {
        var root = FindRepositoryRoot();
        var bashScript = File.ReadAllText(Path.Combine(root, "scripts", "check-ef-drift.sh"));
        var powershellScript = File.ReadAllText(Path.Combine(root, "scripts", "check-ef-drift.ps1"));
        var docs = File.ReadAllText(Path.Combine(root, "docs", "ef-model-drift-check.md"));

        foreach (var script in new[] { bashScript, powershellScript })
        {
            Assert.Contains("has-pending-model-changes", script, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("__ModelDriftCheck", script, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("ConnectionStrings__DefaultConnection", script, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Database__ApplyMigrationsOnStartup", script, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Database__SeedDemoData", script, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("AdminBootstrap__Enabled", script, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Provisioning__LiveExecutionEnabled", script, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Payments__Stripe__Mode", script, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("X3UI_PASSWORD", script, StringComparison.OrdinalIgnoreCase);
        }

        Assert.Contains("check-validation-safety.sh", bashScript, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Remove-Item", powershellScript, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("ReadAllBytes", powershellScript, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("WriteAllBytes", powershellScript, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("scripts\\check-ef-drift.ps1", docs, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("./scripts/check-ef-drift.sh", docs, StringComparison.OrdinalIgnoreCase);
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "README.md"))
                && Directory.Exists(Path.Combine(directory.FullName, "backend"))
                && Directory.Exists(Path.Combine(directory.FullName, "scripts")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Repository root was not found.");
    }
}
