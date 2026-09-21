namespace Application.Security;

/// <summary>
/// Constantes de identidad usadas fuera del catálogo de roles de BD.
/// <see cref="ClientRoleName"/> es el nombre de rol en el JWT delegado del bot
/// (claim <c>role</c>), no un rol persistido en ROLES.
/// </summary>
public static class WebPlatformAccess
{
    public const string ClientRoleName = "Cliente";
}
