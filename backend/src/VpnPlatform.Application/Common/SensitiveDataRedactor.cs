using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace VpnPlatform.Application.Common;

public static class SensitiveDataRedactor
{
    private const string Replacement = "***REDACTED***";

    private static readonly string[] SensitiveJsonPropertyNameFragments =
    {
        "password",
        "passwd",
        "pwd",
        "token",
        "secret",
        "apikey",
        "privatekey",
        "sshkey",
        "sshpass",
        "credential",
        "authorization",
        "bottoken",
        "webhooksecret",
        "x3ui",
        "panelpassword",
        "accesstoken",
        "refreshtoken",
        "clientsecret"
    };

    private static readonly string[] SafeJsonMetadataAffixes =
    {
        "configured",
        "masked",
        "rotated"
    };

    private static readonly Regex[] Patterns =
    {
        new("(?i)(Authorization\\s*:\\s*)(Bearer\\s+)?[^\\s]+", RegexOptions.Compiled),
        new("(?i)(password|passwd|pwd|token|secret|api[_-]?key|private[_-]?key|ssh[_-]?key|ssh[_-]?pass|credential|authorization|bot[_-]?token|webhook[_-]?secret|x3ui|panel[_-]?password)(\\s*[:=]\\s*)([^\\s,;\\}\\]\\\"']+)", RegexOptions.Compiled),
        new("-----BEGIN [A-Z ]*PRIVATE KEY-----[\\s\\S]*?-----END [A-Z ]*PRIVATE KEY-----", RegexOptions.Compiled),
        new("(?i)(v1:)[A-Za-z0-9+/=]+", RegexOptions.Compiled)
    };

    public static string Redact(string? value, IEnumerable<string?>? knownSecrets = null, int? maxLength = null)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        var redacted = TryRedactJson(value);

        foreach (var secret in (knownSecrets ?? Enumerable.Empty<string?>())
                     .Where(secret => !string.IsNullOrWhiteSpace(secret))
                     .Distinct(StringComparer.Ordinal)
                     .OrderByDescending(secret => secret!.Length))
        {
            redacted = redacted.Replace(secret!, Replacement, StringComparison.Ordinal);
        }

        foreach (var pattern in Patterns)
        {
            redacted = pattern.Replace(redacted, match =>
            {
                if (match.Value.StartsWith("-----BEGIN", StringComparison.OrdinalIgnoreCase))
                {
                    return "-----BEGIN PRIVATE KEY-----***REDACTED***-----END PRIVATE KEY-----";
                }

                if (match.Groups.Count >= 4)
                {
                    return match.Groups[1].Value + match.Groups[2].Value + Replacement;
                }

                if (match.Groups.Count >= 2)
                {
                    return match.Groups[1].Value + Replacement;
                }

                return Replacement;
            });
        }

        if (!maxLength.HasValue || redacted.Length <= maxLength.Value)
        {
            return redacted;
        }

        const string truncationSuffix = "\n...[truncated]";
        if (maxLength.Value <= 0)
        {
            return string.Empty;
        }

        if (maxLength.Value <= truncationSuffix.Length)
        {
            return truncationSuffix[..maxLength.Value];
        }

        return redacted[..(maxLength.Value - truncationSuffix.Length)] + truncationSuffix;
    }

    private static string TryRedactJson(string value)
    {
        var firstContentCharacter = value.AsSpan().TrimStart();
        if (firstContentCharacter.IsEmpty || firstContentCharacter[0] is not ('{' or '['))
        {
            return value;
        }

        try
        {
            var root = JsonNode.Parse(value);
            if (root is null)
            {
                return value;
            }

            RedactJsonNode(root);
            return root.ToJsonString();
        }
        catch (JsonException)
        {
            return value;
        }
    }

    private static void RedactJsonNode(JsonNode node)
    {
        if (node is JsonObject jsonObject)
        {
            foreach (var property in jsonObject.ToArray())
            {
                if (IsSensitiveJsonProperty(property.Key))
                {
                    jsonObject[property.Key] = Replacement;
                }
                else if (property.Value is not null)
                {
                    RedactJsonNode(property.Value);
                }
            }

            return;
        }

        if (node is JsonArray jsonArray)
        {
            foreach (var item in jsonArray)
            {
                if (item is not null)
                {
                    RedactJsonNode(item);
                }
            }
        }
    }

    private static bool IsSensitiveJsonProperty(string propertyName)
    {
        var normalized = string.Concat(propertyName.Where(char.IsLetterOrDigit)).ToLowerInvariant();
        if (normalized.StartsWith("rotated", StringComparison.Ordinal)
            || SafeJsonMetadataAffixes.Any(affix => normalized.EndsWith(affix, StringComparison.Ordinal)))
        {
            return false;
        }

        return SensitiveJsonPropertyNameFragments.Any(fragment => normalized.Contains(fragment, StringComparison.Ordinal));
    }
}
