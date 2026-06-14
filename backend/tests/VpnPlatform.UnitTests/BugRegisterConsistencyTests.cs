using Xunit;

namespace VpnPlatform.UnitTests;

public class BugRegisterConsistencyTests
{
    [Fact]
    public void Bug_Register_Should_Not_Keep_Fixed_Local_Frontend_And_Docs_Issues_Open()
    {
        var root = FindRepositoryRoot();
        var roadmap = File.ReadAllText(Path.Combine(root, "docs", "PRODUCT_COMPLETION_ROADMAP.md"));

        Assert.DoesNotContain("| BUG-004 | P1 | Frontend | Нет полного browser E2E по public/cabinet/admin | open |", roadmap, StringComparison.Ordinal);
        Assert.DoesNotContain("| BUG-005 | P1 | Docs | Часть roadmap/docs устарела, возможен mojibake в старых `.md` | open |", roadmap, StringComparison.Ordinal);

        Assert.Contains("| BUG-004 | P1 | Frontend | Нет полного browser E2E по public/cabinet/admin | Исправлено |", roadmap, StringComparison.Ordinal);
        Assert.Contains("P9-TST-008", roadmap, StringComparison.Ordinal);
        Assert.Contains("AllScreensBrowserSmokeTests", roadmap, StringComparison.Ordinal);
        Assert.Contains("npm run e2e:console --prefix frontend", roadmap, StringComparison.Ordinal);

        Assert.Contains("| BUG-005 | P1 | Docs | Часть roadmap/docs устарела, возможен mojibake в старых `.md` | Исправлено |", roadmap, StringComparison.Ordinal);
        Assert.Contains("P10-DOC-005", roadmap, StringComparison.Ordinal);
        Assert.Contains("STATE-014", roadmap, StringComparison.Ordinal);
        Assert.Contains("DocumentationEncodingTests", roadmap, StringComparison.Ordinal);
        Assert.Contains("RoadmapCurrentStateTests", roadmap, StringComparison.Ordinal);
    }

    [Fact]
    public void Bug_Register_Should_Keep_Live_External_Blockers_Open()
    {
        var root = FindRepositoryRoot();
        var roadmap = File.ReadAllText(Path.Combine(root, "docs", "PRODUCT_COMPLETION_ROADMAP.md"));

        foreach (var expectedOpenBug in new[]
                 {
                     "| BUG-001 | P0 | VPS/Admin |",
                     "| BUG-002 | P0 | VPN |",
                     "| BUG-003 | P0 | Payments |",
                     "| BUG-006 | P1 | Provisioning |"
                 })
        {
            Assert.Contains(expectedOpenBug, roadmap, StringComparison.Ordinal);
        }

        Assert.Contains("| BUG-001 | P0 | VPS/Admin | Не подтвержден рабочий вход в админку на VPS | partial |", roadmap, StringComparison.Ordinal);
        Assert.Contains("| BUG-002 | P0 | VPN | Не подтверждена live-выдача через реальный 3x-ui | open |", roadmap, StringComparison.Ordinal);
        Assert.Contains("| BUG-003 | P0 | Payments | Не все payment providers подтверждены live/sandbox smoke | open |", roadmap, StringComparison.Ordinal);
        Assert.Contains("| BUG-006 | P1 | Provisioning | Live Ansible provisioning не production-ready из-за secret materialization | open |", roadmap, StringComparison.Ordinal);
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

        throw new InvalidOperationException("Repository root was not found for bug register consistency tests.");
    }
}
