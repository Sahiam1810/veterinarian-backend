using Application.Common.Results;

namespace Application.Security.Errors;
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
        GenericDescription
    );

    public static readonly Error IdentificationNumberAlreadyExists = new(
        "Authentication.IdentificationNumberAlreadyExists",
        GenericDescription
    );

    public static readonly Error InvalidRegistrationData = new(
        "Authentication.InvalidRegistrationData",
        GenericDescription
    );
}