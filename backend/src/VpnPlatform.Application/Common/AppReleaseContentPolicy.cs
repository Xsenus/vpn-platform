namespace VpnPlatform.Application.Common;

public static class AppReleaseContentPolicy
{
    public const int ReleaseIdMaxLength = 160;
    public const int VersionMaxLength = 40;
    public const int TitleMaxLength = 200;
    public const int SummaryMaxLength = 4000;
    public const int ItemTextMaxLength = 4000;
    public const int MaxItems = 100;

    public static bool IsValidReleaseId(string? value)
    {
        var releaseId = value?.Trim();
        if (string.IsNullOrEmpty(releaseId)
            || releaseId.Length > ReleaseIdMaxLength
            || releaseId[0] == '-'
            || releaseId[^1] == '-')
        {
            return false;
        }

        var previousWasSeparator = false;
        foreach (var character in releaseId)
        {
            var isSeparator = character == '-';
            if (!(character is >= 'a' and <= 'z')
                && !(character is >= '0' and <= '9')
                && !isSeparator)
            {
                return false;
            }

            if (isSeparator && previousWasSeparator)
            {
                return false;
            }

            previousWasSeparator = isSeparator;
        }

        return true;
    }

    public static bool TryNormalizeItemType(string? value, out string normalized)
    {
        normalized = value?.Trim().ToLowerInvariant() ?? string.Empty;
        return normalized is "new" or "improved" or "fixed" or "important";
    }

    public static bool TryNormalizeSource(string? value, string defaultSource, out string normalized)
    {
        normalized = string.IsNullOrWhiteSpace(value)
            ? defaultSource
            : value.Trim().ToLowerInvariant();
        return normalized is "agent" or "manual";
    }

    public static bool TryResolveSortOrder(int? value, int itemIndex, out int resolved)
    {
        if (value < 0 || itemIndex < 0)
        {
            resolved = 0;
            return false;
        }

        resolved = value is > 0 ? value.Value : checked((itemIndex + 1) * 10);
        return true;
    }
}
