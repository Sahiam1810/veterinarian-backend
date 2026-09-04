using Application.Common.Results;

namespace Application.Security.Errors;
public static class AuthenticationErrors
{
    public static readonly Error InvalidCredentials = new(
        "Authentication.InvalidCredentials",
        "Las credenciales proporcionadas son inválidas.");

    public static readonly Error InvalidRefreshToken = new(
        "Authentication.InvalidRefreshToken",
        "El token de actualización es inválido o ha expirado.");

    public static readonly Error UserAlreadyExists = new(
        "Authentication.UserAlreadyExists",
        "El correo electrónico o nombre de usuario ya se encuentra registrado."
    );

    public static readonly Error IdentificationNumberAlreadyExists = new(
        "Authentication.IdentificationNumberAlreadyExists",
        "El número de identificación ya se encuentra registrado."
    );

    public static readonly Error InvalidRegistrationData = new(
        "Authentication.InvalidRegistrationData",
        "Los datos proporcionados para el registro son inválidos o están incompletos."
    );

    public static readonly Error PlatformAccessDenied = new(
        "Authentication.PlatformAccessDenied",
        "No tienes permisos para acceder a esta plataforma."
    );
}