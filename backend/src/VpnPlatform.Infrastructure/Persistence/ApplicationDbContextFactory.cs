using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace VpnPlatform.Infrastructure.Persistence;

public sealed class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";
        var basePath = Directory.GetCurrentDirectory();
        var apiPath = Path.GetFullPath(Path.Combine(basePath, "..", "VpnPlatform.Api"));
        if (!Directory.Exists(apiPath))
        {
            apiPath = Path.GetFullPath(Path.Combine(basePath, "backend", "src", "VpnPlatform.Api"));
        }

        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.Exists(apiPath) ? apiPath : basePath)
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile($"appsettings.{environment}.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        if (configuration.GetConnectionString("DefaultConnection") is null
            && Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection") is string envConnectionString)
        {
            configuration = new ConfigurationBuilder()
                .AddConfiguration(configuration)
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DefaultConnection"] = envConnectionString
                })
                .Build();
        }

        var builder = new DbContextOptionsBuilder<ApplicationDbContext>();
        builder.UseConfiguredDatabase(configuration);
        var options = builder.Options;

        return new ApplicationDbContext(options);
    }
}
