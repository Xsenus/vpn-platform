using System.Text.RegularExpressions;
using Xunit;

namespace VpnPlatform.UnitTests;

public class AdminGuideDocumentationTests
{
    [Fact]
    public void Admin_Guide_Should_Cover_Every_Admin_Navigation_Section()
    {
        var root = FindRepositoryRoot();
        var guide = File.ReadAllText(Path.Combine(root, "docs", "admin-guide.md"));
        var capabilities = File.ReadAllText(Path.Combine(root, "frontend", "apps", "admin-panel", "src", "admin-capabilities.ts"));
        var sectionBlock = Regex.Match(capabilities, @"export const adminSectionLabels[^=]*= \{(?<body>.*?)\n\}", RegexOptions.Singleline);

        Assert.True(sectionBlock.Success, "adminSectionLabels block was not found in admin-capabilities.ts.");

        var sections = Regex.Matches(sectionBlock.Groups["body"].Value, @"(?m)^\s*(?<id>[a-z]+):\s*'(?<label>[^']+)',?$")
            .Select(match => new
            {
                Id = match.Groups["id"].Value,
                Label = match.Groups["label"].Value
            })
            .ToArray();

        Assert.Equal(17, sections.Length);

        foreach (var section in sections)
        {
            Assert.Contains(section.Label, guide, StringComparison.Ordinal);
            Assert.Contains(section.Id, guide, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void Admin_Guide_Should_Cover_Operational_Setup_Scenarios()
    {
        var guide = File.ReadAllText(Path.Combine(FindRepositoryRoot(), "docs", "admin-guide.md"));

        foreach (var provider in new[] { "YooKassa", "RoboKassa", "YooMoney", "CloudPayments", "TBank", "Prodamus", "Stripe", "PayPal", "Telegram Stars" })
        {
            Assert.Contains(provider, guide, StringComparison.OrdinalIgnoreCase);
        }

        foreach (var required in new[]
        {
            "P10-DOC-002",
            "admin-bootstrap",
            "Проверить подключение",
            "write-only",
            "webhook",
            "sandbox",
            "fail-closed",
            "3x-ui",
            "inbound",
            "VLESS",
            "VMess",
            "Trojan",
            "QR",
            "RBAC",
            "Аудит",
            "ReleaseDocumentationGuardTests",
            "Provisioning__LiveExecutionEnabled=true",
            "Rollback",
            "local SQLite smoke"
        })
        {
            Assert.Contains(required, guide, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void Admin_Guide_Should_Not_Contain_Mojibake_Or_Replacement_Characters()
    {
        var guide = File.ReadAllText(Path.Combine(FindRepositoryRoot(), "docs", "admin-guide.md"));
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
            Assert.DoesNotContain(marker, guide, StringComparison.Ordinal);
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

        throw new InvalidOperationException("Repository root was not found for admin guide documentation tests.");
    }
}
