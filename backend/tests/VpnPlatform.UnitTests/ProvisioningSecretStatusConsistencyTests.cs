using Xunit;

namespace VpnPlatform.UnitTests;

public class ProvisioningSecretStatusConsistencyTests
{
    [Fact]
    public void Provisioning_Secret_Documentation_Should_Match_Materializer_Implementation_Status()
    {
        var root = FindRepositoryRoot();
        var roadmap = File.ReadAllText(Path.Combine(root, "docs", "PRODUCT_COMPLETION_ROADMAP.md"));
        var securityHardening = File.ReadAllText(Path.Combine(root, "docs", "SECURITY_HARDENING_MVP.md"));
        var productionSecretStorage = File.ReadAllText(Path.Combine(root, "docs", "production-secret-storage.md"));
        var liveCredentials = File.ReadAllText(Path.Combine(root, "docs", "live-ansible-credentials.md"));
        var materializer = File.ReadAllText(Path.Combine(root, "backend", "src", "VpnPlatform.Infrastructure", "Provisioning", "ProvisioningSecretMaterializer.cs"));
        var executor = File.ReadAllText(Path.Combine(root, "backend", "src", "VpnPlatform.Infrastructure", "Provisioning", "AnsibleProvisioningExecutor.cs"));

        Assert.Contains("| BUG-006 | P1 | Provisioning | Live Ansible provisioning не production-ready из-за secret materialization | Исправлено |", roadmap, StringComparison.Ordinal);
        Assert.Contains("ProvisioningSecretMaterializer", roadmap, StringComparison.Ordinal);
        Assert.Contains("live VPS/provisioning smoke остается отдельным P0/P11-блокером", roadmap, StringComparison.OrdinalIgnoreCase);

        Assert.DoesNotContain("still cannot materialize protected SSH credentials", securityHardening, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("can materialize protected `ssh_key` credentials only through `ProvisioningSecretMaterializer`", securityHardening, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("remain fail-closed until a real staging/VPS run is approved", securityHardening, StringComparison.OrdinalIgnoreCase);

        Assert.Contains("Protected `ssh_key` credentials can be materialized", File.ReadAllText(Path.Combine(root, "docs", "TODO_SECURE_PROVISIONING_SECRETS.md")), StringComparison.OrdinalIgnoreCase);
        Assert.Contains("удаляет файл в `finally`", productionSecretStorage, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("расшифровывает только `ssh-auth:ssh_key`", liveCredentials, StringComparison.OrdinalIgnoreCase);

        Assert.Contains("MaterializeSshPrivateKeyAsync", materializer, StringComparison.Ordinal);
        Assert.Contains("Dispose()", executor, StringComparison.Ordinal);
        Assert.Contains("BuildKnownSecretsForRedaction", executor, StringComparison.Ordinal);

        foreach (var liveBlocker in new[]
                 {
                     "[ ] `P11-ACC-002`",
                     "[ ] `P0-VPN-004`",
                     "[ ] `STATE-012`"
                 })
        {
            Assert.Contains(liveBlocker, roadmap, StringComparison.Ordinal);
        }
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

        throw new InvalidOperationException("Repository root was not found for provisioning secret status consistency tests.");
    }
}
