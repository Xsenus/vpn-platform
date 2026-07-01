using Xunit;

namespace VpnPlatform.UnitTests;

public class ReadmeDocumentationTests
{
    [Fact]
    public void Readme_Should_Be_Russian_And_Cover_Local_Run_Without_Docker()
    {
        var readme = File.ReadAllText(Path.Combine(FindRepositoryRoot(), "README.md"));

        Assert.Contains("# VPN Platform", readme, StringComparison.Ordinal);
        Assert.Contains("Быстрый запуск без Docker", readme, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("powershell -ExecutionPolicy Bypass -File scripts\\start-local.ps1", readme, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("http://127.0.0.1:5173", readme, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("http://127.0.0.1:5174", readme, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("http://127.0.0.1:5175", readme, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("admin@local.test", readme, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("LocalAdminPassword123!", readme, StringComparison.Ordinal);
        Assert.Contains("SQLite", readme, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("PostgreSQL", readme, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Readme_Should_Expose_Current_Validation_Commands_And_Status()
    {
        var readme = File.ReadAllText(Path.Combine(FindRepositoryRoot(), "README.md"));

        Assert.Contains("dotnet test backend\\VpnPlatform.sln --configuration Release", readme, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("dotnet build backend\\src\\VpnPlatform.Api\\VpnPlatform.Api.csproj --configuration Release", readme, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("npm test --prefix frontend", readme, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("npm run typecheck --prefix frontend", readme, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("npm run build --prefix frontend", readme, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("npm audit --audit-level=high --prefix frontend", readme, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("npm run e2e:public --prefix frontend", readme, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("npm run e2e:cabinet --prefix frontend", readme, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("npm run e2e:admin --prefix frontend", readme, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("npm run e2e:all-screens --prefix frontend", readme, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("npm run e2e:mobile --prefix frontend", readme, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("npm run e2e:console --prefix frontend", readme, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("614/614", readme, StringComparison.Ordinal);
        Assert.Contains("66/66", readme, StringComparison.Ordinal);
        Assert.Contains("0 vulnerabilities", readme, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("CHANGELOG.md", readme, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("docs/final-runbook.md", readme, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("docs/release-decision.md", readme, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("2026-07-01-payment-smoke-generator-release-guard", readme, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Readme_Should_Not_Contain_Mojibake_Or_Replacement_Characters()
    {
        var readme = File.ReadAllText(Path.Combine(FindRepositoryRoot(), "README.md"));
        var forbidden = new[]
        {
            "\uFFFD",
            new string([('\u0420'), ('\u040E')]),
            new string([('\u0420'), ('\u045F')]),
            new string([('\u0420'), ('\u0491')]),
            new string([('\u0421'), ('\u0403')]),
            new string([('\u0420'), ('\u00B5'), ('\u0420')])
        };

        foreach (var marker in forbidden)
        {
            Assert.DoesNotContain(marker, readme, StringComparison.Ordinal);
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

        throw new InvalidOperationException("Repository root was not found for README documentation tests.");
    }
}
