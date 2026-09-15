namespace Application.Security;

/// <summary>
/// Cliente no tiene acceso web; solo interactúa vía canal no-panel.
/// Cualquier otro rol (incluido uno configurable nuevo) puede usar el panel.
/// </summary>
public static class WebPlatformAccess
{
    public const string ClientRoleName = "Cliente";

    public static bool IsClientRoleName(string? roleName) =>
        string.Equals(roleName, ClientRoleName, StringComparison.Ordinal);
}
