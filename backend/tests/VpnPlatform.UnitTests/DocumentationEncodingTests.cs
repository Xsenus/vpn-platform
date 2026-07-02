using System.Text;
using Xunit;

namespace VpnPlatform.UnitTests;

public class DocumentationEncodingTests
{
    [Fact]
    public void Russian_Markdown_Documentation_Should_Not_Contain_Mojibake_Markers()
    {
        var root = FindRepositoryRoot();
        var markdownFiles = Directory
            .EnumerateFiles(Path.Combine(root, "docs"), "*.md", SearchOption.AllDirectories)
            .Concat([
                Path.Combine(root, "AGENTS.md"),
                Path.Combine(root, "README.md"),
                Path.Combine(root, "CHANGELOG.md"),
                Path.Combine(root, "TEST_RESULTS.md")
            ])
            .Concat(EnumerateSourceLikeFiles(root))
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        Assert.NotEmpty(markdownFiles);

        var forbiddenMarkers = new[]
        {
            "\uFFFD",
            "\u00D0",
            "\u00D1",
            "\u00C3",
            "\u00C2",
            new string(['\u00E2', '\u20AC']),
            new string(['\u0420', '\u00AD']),
            new string(['\u0420', '\u00A0']),
            new string(['\u0420', '\u0098']),
            new string(['\u0420', '\u0406']),
            new string(['\u0420', '\u040E']),
            new string(['\u0420', '\u2018']),
            new string(['\u0420', '\u045F']),
            new string(['\u0420', '\u0402']),
            new string(['\u0420', '\u0491']),
            new string(['\u0420', '\u00B5']),
            new string(['\u0421', '\u201A']),
            new string(['\u0421', '\u0402'])
        };

        foreach (var file in markdownFiles)
        {
            var lines = File.ReadAllLines(file);
            for (var index = 0; index < lines.Length; index++)
            {
                foreach (var marker in forbiddenMarkers)
                {
                    Assert.False(
                        lines[index].Contains(marker, StringComparison.Ordinal),
                        $"Mojibake marker {Describe(marker)} found in {Path.GetRelativePath(root, file)}:{index + 1}.");
                }
            }
        }
    }

    [Fact]
    public void Agent_Instructions_Should_Cover_Progress_Cleanup_Testing_And_Local_Db()
    {
        var root = FindRepositoryRoot();
        var instructions = File.ReadAllText(Path.Combine(root, "AGENTS.md"));

        foreach (var expected in new[]
                 {
                     "## Roadmap And Progress Reporting",
                     "\u0441\u043A\u043E\u043B\u044C\u043A\u043E \u0437\u0430\u0434\u0430\u0447 \u0432\u044B\u043F\u043E\u043B\u043D\u0435\u043D\u043E",
                     "\u0441\u043A\u043E\u043B\u044C\u043A\u043E \u0437\u0430\u0434\u0430\u0447 \u043E\u0441\u0442\u0430\u043B\u043E\u0441\u044C",
                     "\u043F\u0440\u043E\u0446\u0435\u043D\u0442 \u0433\u043E\u0442\u043E\u0432\u043D\u043E\u0441\u0442\u0438",
                     "\u0432\u044B\u043F\u043E\u043B\u043D\u0435\u043D\u043E / \u0432\u0441\u0435\u0433\u043E * 100",
                     "\u0443\u043A\u0430\u0437\u044B\u0432\u0430\u0442\u044C \u0438\u0441\u0442\u043E\u0447\u043D\u0438\u043A",
                     "\u0434\u0430\u0442\u0443/\u0432\u0435\u0440\u0441\u0438\u044E \u0441\u0442\u0430\u0442\u0443\u0441\u0430",
                     "## Artifact Cleanup",
                     "\u0443\u0431\u0440\u0430\u0442\u044C \u0437\u0430 \u0441\u043E\u0431\u043E\u0439 \u0432\u0440\u0435\u043C\u0435\u043D\u043D\u044B\u0435 \u0430\u0440\u0442\u0435\u0444\u0430\u043A\u0442\u044B",
                     "\u043D\u0435\u0442 \u0441\u0435\u043A\u0440\u0435\u0442\u043E\u0432",
                     "## Testing Requirements",
                     "\u043A\u0430\u0436\u0434\u0430\u044F \u0434\u043E\u0431\u0430\u0432\u043B\u0435\u043D\u043D\u0430\u044F \u0438\u043B\u0438 \u0438\u0437\u043C\u0435\u043D\u0435\u043D\u043D\u0430\u044F \u0444\u0443\u043D\u043A\u0446\u0438\u044F",
                     "\u043B\u043E\u043A\u0430\u043B\u044C\u043D\u0430\u044F \u0411\u0414",
                     "\u043B\u043E\u043A\u0430\u043B\u044C\u043D\u044B\u0439 SQLite-\u0440\u0435\u0436\u0438\u043C"
                 })
        {
            Assert.Contains(expected, instructions, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void Agent_Instructions_Should_Require_Source_And_Date_Version_For_Roadmap_Status()
    {
        var root = FindRepositoryRoot();
        var instructions = File.ReadAllText(Path.Combine(root, "AGENTS.md"));

        foreach (var expected in new[]
                 {
                     "\u0434\u0430\u043D\u043D\u044B\u0435 \u0431\u0435\u0440\u0443\u0442\u0441\u044F \u0438\u0437 roadmap",
                     "\u0443\u043A\u0430\u0437\u044B\u0432\u0430\u0442\u044C \u0438\u0441\u0442\u043E\u0447\u043D\u0438\u043A",
                     "\u0434\u0430\u0442\u0443/\u0432\u0435\u0440\u0441\u0438\u044E \u0441\u0442\u0430\u0442\u0443\u0441\u0430"
                 })
        {
            Assert.Contains(expected, instructions, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void Agent_Instructions_Should_Require_Unavailable_Checks_And_Residual_Risk_In_Final_Answer()
    {
        var root = FindRepositoryRoot();
        var instructions = File.ReadAllText(Path.Combine(root, "AGENTS.md"));

        foreach (var expected in new[]
                 {
                     "\u0415\u0441\u043B\u0438 \u0442\u0435\u0441\u0442, \u043B\u043E\u043A\u0430\u043B\u044C\u043D\u0430\u044F \u0411\u0414 \u0438\u043B\u0438 \u0432\u043D\u0435\u0448\u043D\u044F\u044F \u043F\u0440\u043E\u0432\u0435\u0440\u043A\u0430 \u043D\u0435\u0434\u043E\u0441\u0442\u0443\u043F\u043D\u044B",
                     "\u0447\u0442\u043E \u043D\u0435 \u0431\u044B\u043B\u043E \u043F\u0440\u043E\u0432\u0435\u0440\u0435\u043D\u043E",
                     "\u043F\u043E\u0447\u0435\u043C\u0443",
                     "\u043E\u0441\u0442\u0430\u0442\u043E\u0447\u043D\u044B\u0439 \u0440\u0438\u0441\u043A"
                 })
        {
            Assert.Contains(expected, instructions, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void Agent_Instructions_Should_Require_Russian_Commit_And_No_Push_Without_Request()
    {
        var root = FindRepositoryRoot();
        var instructions = File.ReadAllText(Path.Combine(root, "AGENTS.md"));

        foreach (var expected in new[]
                 {
                     "## Git Delivery",
                     "\u0441\u043E\u043E\u0431\u0449\u0435\u043D\u0438\u0435 \u043A\u043E\u043C\u043C\u0438\u0442\u0430 \u043F\u0438\u0441\u0430\u0442\u044C \u043D\u0430 \u0440\u0443\u0441\u0441\u043A\u043E\u043C \u044F\u0437\u044B\u043A\u0435",
                     "\u041D\u0435 \u0432\u044B\u043F\u043E\u043B\u043D\u044F\u0442\u044C push",
                     "\u043F\u043E\u043B\u044C\u0437\u043E\u0432\u0430\u0442\u0435\u043B\u044C \u044F\u0432\u043D\u043E \u043D\u0435 \u0441\u043A\u0430\u0436\u0435\u0442 \u043F\u0443\u0448\u0438\u0442\u044C",
                     "git status",
                     "\u0442\u043E\u043B\u044C\u043A\u043E \u0444\u0430\u0439\u043B\u044B, \u043E\u0442\u043D\u043E\u0441\u044F\u0449\u0438\u0435\u0441\u044F \u043A \u0432\u044B\u043F\u043E\u043B\u043D\u0435\u043D\u043D\u043E\u0439 \u0437\u0430\u0434\u0430\u0447\u0435"
                 })
        {
            Assert.Contains(expected, instructions, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void Agent_Instructions_Should_Cover_Image_Attachments_And_Missing_Attachments()
    {
        var root = FindRepositoryRoot();
        var instructions = File.ReadAllText(Path.Combine(root, "AGENTS.md"));

        foreach (var expected in new[]
                 {
                     "## Image And Screenshot Inputs",
                     "\u043F\u0440\u0438\u043A\u0440\u0435\u043F\u043B\u0435\u043D\u043D\u044B\u0435 \u0444\u043E\u0442\u043E, \u0441\u043A\u0440\u0438\u043D\u044B \u0438\u043B\u0438 \u0438\u0437\u043E\u0431\u0440\u0430\u0436\u0435\u043D\u0438\u044F",
                     "\u0434\u043E\u0441\u0442\u0443\u043F\u043D\u044B \u043B\u0438 \u0432\u043B\u043E\u0436\u0435\u043D\u0438\u044F",
                     "\u0442\u0435\u043A\u0441\u0442 \u043D\u0430 \u0438\u0437\u043E\u0431\u0440\u0430\u0436\u0435\u043D\u0438\u044F\u0445",
                     "\u043F\u043E\u043C\u0435\u0442\u043A\u0438 \u0437\u0430\u043A\u0430\u0437\u0447\u0438\u043A\u0430",
                     "\u043D\u0435 \u0432\u044B\u0434\u0443\u043C\u044B\u0432\u0430\u0442\u044C \u0442\u0435\u043A\u0441\u0442 \u0441 \u0438\u0437\u043E\u0431\u0440\u0430\u0436\u0435\u043D\u0438\u0439",
                     "\u0438\u0437\u043E\u0431\u0440\u0430\u0436\u0435\u043D\u0438\u044F \u043D\u0435 \u0431\u044B\u043B\u0438 \u0434\u043E\u0441\u0442\u0443\u043F\u043D\u044B"
                 })
        {
            Assert.Contains(expected, instructions, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void Agent_Instructions_Should_Deduplicate_Completed_And_Partial_Tasks()
    {
        var root = FindRepositoryRoot();
        var instructions = File.ReadAllText(Path.Combine(root, "AGENTS.md"));

        foreach (var expected in new[]
                 {
                     "## Duplicate And Completed Tasks",
                     "\u043D\u0435 \u0431\u044B\u043B\u0430 \u043B\u0438 \u043E\u043D\u0430 \u0443\u0436\u0435 \u0437\u0430\u043A\u0440\u044B\u0442\u0430",
                     "roadmap",
                     "changelog",
                     "TEST_RESULTS",
                     "\u0447\u0442\u043E \u043D\u043E\u0432\u043E\u0433\u043E",
                     "\u043A\u043E\u0434\u0435",
                     "\u043D\u0435 \u043F\u0435\u0440\u0435\u043E\u0442\u043A\u0440\u044B\u0432\u0430\u0442\u044C",
                     "\u043D\u0435 \u0434\u0443\u0431\u043B\u0438\u0440\u043E\u0432\u0430\u0442\u044C \u0438\u0437\u043C\u0435\u043D\u0435\u043D\u0438\u044F",
                     "\u0432\u044B\u043F\u043E\u043B\u043D\u0435\u043D\u0430 \u0447\u0430\u0441\u0442\u0438\u0447\u043D\u043E",
                     "\u0442\u043E\u043B\u044C\u043A\u043E \u043D\u0435\u0434\u043E\u0441\u0442\u0430\u044E\u0449\u0443\u044E \u0447\u0430\u0441\u0442\u044C"
                 })
        {
            Assert.Contains(expected, instructions, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void Agent_Instructions_Should_Require_Local_Db_For_User_Api_Payment_Vpn_Admin_Cabinet_And_Provisioning()
    {
        var root = FindRepositoryRoot();
        var instructions = File.ReadAllText(Path.Combine(root, "AGENTS.md"));

        foreach (var expected in new[]
                 {
                     "\u043B\u043E\u043A\u0430\u043B\u044C\u043D\u0430\u044F \u0411\u0414",
                     "\u043B\u043E\u043A\u0430\u043B\u044C\u043D\u044B\u0439 SQLite-\u0440\u0435\u0436\u0438\u043C",
                     "\u043F\u043E\u043B\u044C\u0437\u043E\u0432\u0430\u0442\u0435\u043B\u044C\u0441\u043A\u0438\u0435",
                     "API",
                     "payment",
                     "VPN",
                     "admin",
                     "cabinet",
                     "provisioning",
                     "\u0441\u0446\u0435\u043D\u0430\u0440\u0438\u0438 \u043D\u0443\u0436\u043D\u043E \u043F\u0440\u043E\u0432\u0435\u0440\u044F\u0442\u044C \u043D\u0430 \u043B\u043E\u043A\u0430\u043B\u044C\u043D\u043E\u0439 \u0411\u0414"
                 })
        {
            Assert.Contains(expected, instructions, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void Agent_Instructions_Should_Require_Verification_Whats_New_Cleanup_And_Status_Before_Commit()
    {
        var root = FindRepositoryRoot();
        var instructions = File.ReadAllText(Path.Combine(root, "AGENTS.md"));

        foreach (var expected in new[]
                 {
                     "## Verification And Release Handoff",
                     "\u041F\u043E\u0441\u043B\u0435 \u0440\u0435\u0430\u043B\u0438\u0437\u0430\u0446\u0438\u0438 \u0438 \u0438\u0441\u043F\u0440\u0430\u0432\u043B\u0435\u043D\u0438\u044F \u043E\u0448\u0438\u0431\u043E\u043A \u0441\u043D\u0430\u0447\u0430\u043B\u0430 \u0437\u0430\u0432\u0435\u0440\u0448\u0438\u0442\u044C \u043F\u0440\u043E\u0432\u0435\u0440\u043A\u0438",
                     "\u043B\u043E\u043A\u0430\u043B\u044C\u043D\u0443\u044E \u0411\u0414/SQLite",
                     "\u0427\u0442\u043E \u043D\u043E\u0432\u043E\u0433\u043E",
                     "\u043D\u0435\u0434\u043E\u0441\u0442\u0443\u043F\u043D\u044B\u0445 \u043F\u0440\u043E\u0432\u0435\u0440\u043E\u043A",
                     "\u043E\u0441\u0442\u0430\u0442\u043E\u0447\u043D\u043E\u0433\u043E \u0440\u0438\u0441\u043A\u0430",
                     "\u0441\u0438\u043D\u0445\u0440\u043E\u043D\u0438\u0437\u0430\u0446\u0438\u0438 roadmap/status-\u0434\u043E\u043A\u0443\u043C\u0435\u043D\u0442\u043E\u0432",
                     "\u043E\u0447\u0438\u0441\u0442\u043A\u0438 \u0430\u0440\u0442\u0435\u0444\u0430\u043A\u0442\u043E\u0432",
                     "\u0444\u0438\u043D\u0430\u043B\u044C\u043D\u043E\u0439 \u043F\u0440\u043E\u0432\u0435\u0440\u043A\u0438 `git status`"
                 })
        {
            Assert.Contains(expected, instructions, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void Agent_Instructions_Should_Require_Encoding_Verification_For_Text_And_Release_Seed_Changes()
    {
        var root = FindRepositoryRoot();
        var instructions = File.ReadAllText(Path.Combine(root, "AGENTS.md"));

        foreach (var expected in new[]
                 {
                     "## Encoding Verification",
                     "\u043F\u0440\u043E\u0432\u0435\u0440\u044F\u0442\u044C \u043A\u043E\u0434\u0438\u0440\u043E\u0432\u043A\u0443",
                     "markdown",
                     "JSON",
                     "C#",
                     "TypeScript",
                     "strict UTF-8 without BOM",
                     "mojibake markers",
                     "\u0440\u0443\u0441\u0441\u043A\u043E\u044F\u0437\u044B\u0447\u043D\u044B\u0435 \u0442\u0435\u043A\u0441\u0442\u044B",
                     "roadmap/status-\u0434\u043E\u043A\u0443\u043C\u0435\u043D\u0442\u044B",
                     "release seed",
                     "encoding guard"
                 })
        {
            Assert.Contains(expected, instructions, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void Documentation_And_Release_Seed_Should_Be_Strict_Utf8_Without_Bom()
    {
        var root = FindRepositoryRoot();
        var files = Directory
            .EnumerateFiles(Path.Combine(root, "docs"), "*.md", SearchOption.AllDirectories)
            .Concat([
                Path.Combine(root, "AGENTS.md"),
                Path.Combine(root, "README.md"),
                Path.Combine(root, "CHANGELOG.md"),
                Path.Combine(root, "TEST_RESULTS.md"),
                Path.Combine(root, "backend", "src", "VpnPlatform.Api", "AppReleases", "releases.json")
            ])
            .Concat(EnumerateSourceLikeFiles(root))
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var strictUtf8 = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

        Assert.NotEmpty(files);
        foreach (var file in files)
        {
            var bytes = File.ReadAllBytes(file);
            Assert.NotEmpty(bytes);
            Assert.False(
                bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF,
                $"{Path.GetRelativePath(root, file)} must be UTF-8 without BOM.");

            _ = strictUtf8.GetString(bytes);
        }
    }

    private static string Describe(string marker)
        => string.Join(" ", marker.Select(character => $"U+{(int)character:X4}"));

    private static IEnumerable<string> EnumerateSourceLikeFiles(string root)
    {
        var sourceExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".cs",
            ".ts",
            ".tsx",
            ".js",
            ".mjs",
            ".css",
            ".html",
            ".md",
            ".json",
            ".csproj",
            ".sln",
            ".props",
            ".targets",
            ".http",
            ".config",
            ".conf",
            ".ini",
            ".xml",
            ".j2",
            ".yml",
            ".yaml",
            ".py",
            ".ps1",
            ".sh"
        };
        var sourceFileNamesWithoutExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".dockerignore",
            ".editorconfig",
            ".env.example",
            ".gitattributes",
            ".gitignore"
        };

        return Directory
            .EnumerateFiles(root, "*", SearchOption.AllDirectories)
            .Where(file =>
            {
                var relativePath = Path.GetRelativePath(root, file);
                var fileName = Path.GetFileName(file);
                return (sourceExtensions.Contains(Path.GetExtension(file))
                       || sourceFileNamesWithoutExtensions.Contains(fileName)
                       || fileName.Equals("Dockerfile", StringComparison.OrdinalIgnoreCase)
                       || fileName.StartsWith("Dockerfile.", StringComparison.OrdinalIgnoreCase))
                       && !relativePath.StartsWith($".git{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase)
                       && !relativePath.StartsWith($".serena{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase)
                       && !relativePath.Contains($"{Path.DirectorySeparatorChar}Migrations{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase)
                       && !relativePath.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase)
                       && !relativePath.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase)
                       && !relativePath.Contains($"{Path.DirectorySeparatorChar}node_modules{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase)
                       && !relativePath.Contains($"{Path.DirectorySeparatorChar}dist{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase);
            });
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "README.md")) && Directory.Exists(Path.Combine(directory.FullName, "docs")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Repository root was not found for documentation encoding tests.");
    }
}
