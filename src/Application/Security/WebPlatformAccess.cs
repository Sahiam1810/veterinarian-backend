using Domain.Roles;

namespace Application.Security;

/// <summary>
/// Roles admitidos en el panel web (email/password + JWT).
/// Cliente no tiene acceso web; solo interactúa vía canal no-panel.
/// </summary>
public static class WebPlatformAccess
{
    public const string ClientRoleName = "Cliente";

    private static readonly HashSet<string> AllowedRoleNames =
        new(StringComparer.Ordinal)
        {
            SystemRoles.SuperAdminName,
            "Administrador",
            "Veterinario",
            "Recepcionista",
            "Auxiliar"
        };

    public static bool IsAllowedRoleName(string? roleName) =>
        roleName is not null && AllowedRoleNames.Contains(roleName);

    public static bool IsClientRoleName(string? roleName) =>
        string.Equals(roleName, ClientRoleName, StringComparison.Ordinal);
}
