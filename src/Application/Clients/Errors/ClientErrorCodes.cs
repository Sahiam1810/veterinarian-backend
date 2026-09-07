namespace Application.Clients.Errors;

// Códigos estables de Clients para problem+json (front).
public static class ClientErrorCodes
{
    public const string PhoneRequired = "Clients.PhoneRequired";
    public const string PhoneInvalidFormat = "Clients.PhoneInvalidFormat";
    // Teléfono ya asociado a otro perfil CLIENTS.
    public const string PhoneAlreadyInUse = "Clients.PhoneAlreadyInUse";
}
