using System.Runtime.InteropServices;
using VpnPlatform.Application.Abstractions;
using VpnPlatform.Domain.Entities;

namespace VpnPlatform.Infrastructure.Provisioning;

public sealed class ProvisioningSecretMaterializer
{
    private readonly ISecretProtector _secretProtector;

    public ProvisioningSecretMaterializer(ISecretProtector secretProtector)
    {
        _secretProtector = secretProtector;
    }

    public async Task<MaterializedProvisioningSecret?> MaterializeSshPrivateKeyAsync(VpnNode node, string workDirectory, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(node.ProtectedSshCredential))
        {
            if (!string.IsNullOrWhiteSpace(node.SshCredentialRef))
            {
                throw new InvalidOperationException("SSH credential reference is configured, but protected SSH credential payload is missing.");
            }

            return null;
        }

        if (node.ProtectedSshCredential.StartsWith("validation-placeholder:", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Validation placeholder SSH credentials cannot be materialized for live Ansible.");
        }

        if (!node.ProtectedSshCredential.StartsWith("v1:", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Protected SSH credential has unsupported format.");
        }

        var authMethod = ResolveSshAuthMethod(node);
        if (!string.Equals(authMethod, "ssh_key", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Live protected SSH password materialization is not supported by the current Ansible runner. Use ssh_key credentials for live provisioning.");
        }

        var plaintext = _secretProtector.Unprotect(node.ProtectedSshCredential);
        if (string.IsNullOrWhiteSpace(plaintext))
        {
            throw new InvalidOperationException("Protected SSH private key is empty.");
        }

        var secretsDirectory = Path.Combine(workDirectory, "secrets");
        Directory.CreateDirectory(secretsDirectory);
        TrySetDirectoryPermissions(secretsDirectory);

        var keyPath = Path.Combine(secretsDirectory, $"ssh-key-{node.Id:N}");
        await File.WriteAllTextAsync(keyPath, NormalizePrivateKey(plaintext), cancellationToken);
        TrySetFilePermissions(keyPath);

        return new MaterializedProvisioningSecret(keyPath, plaintext);
    }

    private static string ResolveSshAuthMethod(VpnNode node)
    {
        var tag = node.TagsCsv
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(x => x.Split(':', 2, StringSplitOptions.TrimEntries))
            .FirstOrDefault(x => x.Length == 2 && string.Equals(x[0], "ssh-auth", StringComparison.OrdinalIgnoreCase));

        return tag?[1] ?? "ssh_key";
    }

    private static string NormalizePrivateKey(string privateKey)
    {
        var normalized = privateKey.Replace("\r\n", "\n", StringComparison.Ordinal).TrimEnd();
        return normalized + "\n";
    }

    private static void TrySetDirectoryPermissions(string path)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return;
        }

        try
        {
            File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        }
        catch
        {
            // Permission hardening is best-effort on filesystems that do not support Unix modes.
        }
    }

    private static void TrySetFilePermissions(string path)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return;
        }

        try
        {
            File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite);
        }
        catch
        {
            // Permission hardening is best-effort on filesystems that do not support Unix modes.
        }
    }
}

public sealed class MaterializedProvisioningSecret : IDisposable
{
    public MaterializedProvisioningSecret(string path, string plaintext)
    {
        Path = path;
        Plaintext = plaintext;
    }

    public string Path { get; }
    public string Plaintext { get; }

    public void Dispose()
    {
        TryDelete(Path);
        var directory = System.IO.Path.GetDirectoryName(Path);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            TryDeleteDirectory(directory);
        }
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch
        {
            // Best-effort cleanup. Callers still redact plaintext from persisted logs.
        }
    }

    private static void TryDeleteDirectory(string path)
    {
        try
        {
            if (Directory.Exists(path) && !Directory.EnumerateFileSystemEntries(path).Any())
            {
                Directory.Delete(path);
            }
        }
        catch
        {
            // Best-effort cleanup.
        }
    }
}
