namespace VpnPlatform.Application.Common;

public static class UserRoles
{
    public const string User = "User";
    public const string SuperAdmin = "SuperAdmin";
    public const string Admin = "Admin";
    public const string SupportAgent = "SupportAgent";
    public const string Operator = "Operator";
    public const string FinanceManager = "FinanceManager";
    public const string ReadOnly = "ReadOnly";

    private static readonly string[] KnownRoles =
    {
        User,
        SuperAdmin,
        Admin,
        SupportAgent,
        Operator,
        FinanceManager,
        ReadOnly
    };

    public static IReadOnlyCollection<string> Parse(string? rolesCsv)
    {
        var roles = (rolesCsv ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(x => KnownRoles.Contains(x, StringComparer.Ordinal))
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        return roles.Length == 0 ? new[] { User } : roles;
    }

    public static string NormalizeCsv(string? rolesCsv)
        => string.Join(',', Parse(rolesCsv));
}
