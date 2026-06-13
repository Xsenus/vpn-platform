using Xunit;

namespace VpnPlatform.UnitTests;

public class UserGuideDocumentationTests
{
    [Fact]
    public void User_Guide_Should_Cover_User_Journey()
    {
        var guide = File.ReadAllText(Path.Combine(FindRepositoryRoot(), "docs", "user-guide.md"));

        foreach (var required in new[]
        {
            "P10-DOC-003",
            "/help",
            "Тарифы",
            "оплат",
            "личный кабинет",
            "ссылка подключения",
            "QR-код",
            "VLESS",
            "VMess",
            "Trojan",
            "Продление",
            "Telegram",
            "Поддержка",
            "Что нового"
        })
        {
            Assert.Contains(required, guide, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void User_Help_Should_Be_Exposed_In_Public_And_Cabinet_Frontend()
    {
        var root = FindRepositoryRoot();
        var publicApp = File.ReadAllText(Path.Combine(root, "frontend", "apps", "public-web", "src", "App.tsx"));
        var cabinetApp = File.ReadAllText(Path.Combine(root, "frontend", "apps", "cabinet", "src", "App.tsx"));

        Assert.Contains("path=\"/help\"", publicApp, StringComparison.Ordinal);
        Assert.Contains("UserHelpPage", publicApp, StringComparison.Ordinal);
        Assert.Contains("Как купить и подключить VPN", publicApp, StringComparison.Ordinal);
        Assert.Contains("После оплаты вернитесь в кабинет", publicApp, StringComparison.Ordinal);

        Assert.Contains("Как пользоваться сервисом", cabinetApp, StringComparison.Ordinal);
        Assert.Contains("Скопируйте ссылку или откройте QR-код", cabinetApp, StringComparison.Ordinal);
        Assert.Contains("Создайте обращение в поддержку", cabinetApp, StringComparison.Ordinal);
    }

    [Fact]
    public void User_Guide_Should_Not_Contain_Mojibake_Or_Replacement_Characters()
    {
        var root = FindRepositoryRoot();
        var files = new[]
        {
            Path.Combine(root, "docs", "user-guide.md"),
            Path.Combine(root, "backend", "tests", "VpnPlatform.UnitTests", "UserGuideDocumentationTests.cs")
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

        throw new InvalidOperationException("Repository root was not found for user guide documentation tests.");
    }
}
