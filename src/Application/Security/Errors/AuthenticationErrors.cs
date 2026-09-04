using Application.Common.Results;

namespace Application.Security.Errors;

// Catálogo de códigos estables de auth. El front traduce por `code`, no por Description.
// Description es texto neutro de respaldo (mismo mensaje genérico); no hardcodear UX por endpoint.
public static class AuthenticationErrors
{
    private const string GenericDescription = "Authentication failed.";

    public static readonly Error InvalidCredentials = new(
        "Authentication.InvalidCredentials",
        GenericDescription);

    public static readonly Error InvalidRefreshToken = new(
        "Authentication.InvalidRefreshToken",
        GenericDescription);

    public static readonly Error UserAlreadyExists = new(
        "Authentication.UserAlreadyExists",
        GenericDescription);

    public static readonly Error IdentificationNumberAlreadyExists = new(
        "Authentication.IdentificationNumberAlreadyExists",
        GenericDescription);

    public static readonly Error InvalidRegistrationData = new(
        "Authentication.InvalidRegistrationData",
        GenericDescription);

    // 401 JWT: token ausente/inválido (JwtBearer OnChallenge).
    public static readonly Error Unauthorized = new(
        "Authentication.Unauthorized",
        GenericDescription);

    // 403 JWT/policy: autenticado pero sin permiso de recurso.
    public static readonly Error Forbidden = new(
        "Authentication.Forbidden",
        GenericDescription);

    // Denegación de acceso a la plataforma (p. ej. rol no admitido en ese front).
    public static readonly Error PlatformAccessDenied = new(
        "Authentication.PlatformAccessDenied",
        GenericDescription);
}
