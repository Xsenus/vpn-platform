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
                Path.Combine(root, "README.md"),
                Path.Combine(root, "TEST_RESULTS.md")
            ])
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

    private static string Describe(string marker)
        => string.Join(" ", marker.Select(character => $"U+{(int)character:X4}"));

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
