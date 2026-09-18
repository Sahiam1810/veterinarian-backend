namespace Application.Permissions.Claims;

public static class PermissionClaimValue
{
    public const string ClaimType = "permissions";
    public const string PolicyPrefix = "perm:";

    public static string Create(string moduleName, string action) =>
        $"{PolicyPrefix}{moduleName}:{action}";

    public static bool TryParse(
        string value,
        out string moduleName,
        out string action)
    {
        moduleName = string.Empty;
        action = string.Empty;

        if (string.IsNullOrWhiteSpace(value) ||
            !value.StartsWith(PolicyPrefix, StringComparison.Ordinal))
        {
            return false;
        }

        var remainder = value[PolicyPrefix.Length..];
        var separatorIndex = remainder.LastIndexOf(':');
        if (separatorIndex <= 0 || separatorIndex == remainder.Length - 1)
        {
            return false;
        }

        moduleName = remainder[..separatorIndex];
        action = remainder[(separatorIndex + 1)..];
        return true;
    }
}
