namespace Application.UserCredentials.Errors;

// Códigos estables de UserCredentials para problem+json (el front traduce por code).
public static class UserCredentialErrorCodes
{
    // Cliente no puede tener USER_CREDENTIALS ni login de plataforma.
    public const string ClientCannotHaveLogin = "UserCredentials.ClientCannotHaveLogin";
}
