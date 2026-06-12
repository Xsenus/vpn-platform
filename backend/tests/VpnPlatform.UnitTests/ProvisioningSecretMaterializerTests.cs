using System.Runtime.InteropServices;
using System.Text;
using VpnPlatform.Application.Abstractions;
using VpnPlatform.Domain.Entities;
using VpnPlatform.Infrastructure.Provisioning;
using Xunit;

namespace VpnPlatform.UnitTests;

public class ProvisioningSecretMaterializerTests
{
    [Fact]
    public async Task MaterializeSshPrivateKeyAsync_Should_Write_Protected_Key_To_Temporary_File_And_Cleanup()
    {
        using var temp = new TempDirectory();
        var protector = new TestSecretProtector();
        var privateKey = "-----BEGIN OPENSSH PRIVATE KEY-----\nsecret-key-material\n-----END OPENSSH PRIVATE KEY-----";
        var node = new VpnNode
        {
            Id = Guid.NewGuid(),
            TagsCsv = "ssh-auth:ssh_key,credentials:protected",
            ProtectedSshCredential = protector.Protect(privateKey),
            SshCredentialRef = "secretref:ssh:test"
        };
        var materializer = new ProvisioningSecretMaterializer(protector);

        using var materialized = await materializer.MaterializeSshPrivateKeyAsync(node, temp.Path, CancellationToken.None);

        Assert.NotNull(materialized);
        Assert.True(File.Exists(materialized.Path));
        Assert.Equal(privateKey + "\n", await File.ReadAllTextAsync(materialized.Path));
        Assert.Equal(privateKey, materialized.Plaintext);
        Assert.DoesNotContain("secret-key-material", materialized.Path, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("secrets", materialized.Path, StringComparison.OrdinalIgnoreCase);

        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            var mode = File.GetUnixFileMode(materialized.Path);
            Assert.False(mode.HasFlag(UnixFileMode.GroupRead));
            Assert.False(mode.HasFlag(UnixFileMode.OtherRead));
        }
    }

    [Fact]
    public async Task MaterializedProvisioningSecret_Dispose_Should_Delete_Temporary_Key_File()
    {
        using var temp = new TempDirectory();
        var protector = new TestSecretProtector();
        var node = new VpnNode
        {
            Id = Guid.NewGuid(),
            TagsCsv = "ssh-auth:ssh_key",
            ProtectedSshCredential = protector.Protect("private-key")
        };
        var materializer = new ProvisioningSecretMaterializer(protector);
        var materialized = await materializer.MaterializeSshPrivateKeyAsync(node, temp.Path, CancellationToken.None);
        Assert.NotNull(materialized);
        var path = materialized.Path;

        materialized.Dispose();

        Assert.False(File.Exists(path));
    }

    [Theory]
    [InlineData("ssh-auth:password", "Live protected SSH password materialization is not supported")]
    [InlineData("ssh-auth:ssh_key", "Validation placeholder SSH credentials cannot be materialized")]
    public async Task MaterializeSshPrivateKeyAsync_Should_Fail_Closed_For_Unsupported_Protected_Credentials(string tagsCsv, string expectedError)
    {
        using var temp = new TempDirectory();
        var protector = new TestSecretProtector();
        var protectedCredential = tagsCsv.Contains("password", StringComparison.OrdinalIgnoreCase)
            ? protector.Protect("ssh-password")
            : "validation-placeholder:test";
        var node = new VpnNode
        {
            Id = Guid.NewGuid(),
            TagsCsv = tagsCsv,
            ProtectedSshCredential = protectedCredential,
            SshCredentialRef = "secretref:ssh:test"
        };
        var materializer = new ProvisioningSecretMaterializer(protector);

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => materializer.MaterializeSshPrivateKeyAsync(node, temp.Path, CancellationToken.None));

        Assert.Contains(expectedError, error.Message, StringComparison.OrdinalIgnoreCase);
        Assert.False(Directory.Exists(System.IO.Path.Combine(temp.Path, "secrets")));
    }

    private sealed class TempDirectory : IDisposable
    {
        public TempDirectory()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"vpnplatform-secret-test-{Guid.NewGuid():N}");
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public void Dispose()
        {
            if (Directory.Exists(Path))
            {
                Directory.Delete(Path, recursive: true);
            }
        }
    }

    private sealed class TestSecretProtector : ISecretProtector
    {
        public string Protect(string plaintext) => "v1:" + Convert.ToBase64String(Encoding.UTF8.GetBytes(plaintext));
        public string Unprotect(string protectedValue) => Encoding.UTF8.GetString(Convert.FromBase64String(protectedValue[3..]));
        public string Mask(string? value, int visibleTail = 4) => string.IsNullOrWhiteSpace(value) ? string.Empty : "***";
    }
}
