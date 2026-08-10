namespace VpnPlatform.Application.Common;

public static class SafeHttpUrl
{
    public static bool TryNormalize(string? value, out string normalized)
    {
        normalized = value?.Trim() ?? string.Empty;
        return Uri.TryCreate(normalized, UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
            && string.IsNullOrEmpty(uri.UserInfo)
            && !string.IsNullOrWhiteSpace(uri.Host);
    }

    public static bool ContainsCredentials(string? value)
        => Uri.TryCreate(value?.Trim(), UriKind.Absolute, out var uri)
            && !string.IsNullOrEmpty(uri.UserInfo);
}
