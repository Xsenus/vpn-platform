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
}
