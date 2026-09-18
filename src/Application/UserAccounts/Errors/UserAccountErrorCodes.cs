namespace Application.UserAccounts.Errors;

// Códigos estables de error de UserAccounts para el front (problem+json).
public static class UserAccountErrorCodes
{
    // Cliente no puede tener USER_ACCOUNTS ni login staff.
    public const string ClientCannotHaveLogin = "UserAccounts.ClientCannotHaveLogin";
}
