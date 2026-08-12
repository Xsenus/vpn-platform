namespace VpnPlatform.Application.Common;

public static class VpnProtocolPolicy
{
    public const string DefaultSupportedProtocolsCsv = "vless,vmess,trojan";

    public static bool IsSupported(string? protocol)
        => Normalize(protocol) is "vless" or "vmess" or "trojan";

    public static string Normalize(string? protocol)
        => string.IsNullOrWhiteSpace(protocol) ? string.Empty : protocol.Trim().ToLowerInvariant();

    public static bool TryNormalizeCsv(string? value, out string normalized)
    {
        var tokens = (value ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(Normalize)
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        if (tokens.Length == 0 || tokens.Any(token => !IsSupported(token)))
        {
            normalized = string.Empty;
            return false;
        }

        normalized = string.Join(',', new[] { "vless", "vmess", "trojan" }.Where(tokens.Contains));
        return true;
    }

    public static bool Supports(string? supportedProtocolsCsv, string? requiredProtocol)
    {
        var required = Normalize(requiredProtocol);
        if (!IsSupported(required))
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(supportedProtocolsCsv))
        {
            return true;
        }

        return supportedProtocolsCsv
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(Normalize)
            .Any(protocol => string.Equals(protocol, required, StringComparison.Ordinal));
    }
}
