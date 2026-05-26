using VpnPlatform.Application.Common;

namespace VpnPlatform.Infrastructure.Security;

public static class SecretRedactor
{
    public static string Redact(string? value, IEnumerable<string?>? knownSecrets = null)
        => SensitiveDataRedactor.Redact(value, knownSecrets);
}
