namespace Application.Security.Models;

// U5: USERS fusiona lo que antes eran USER_ACCOUNTS/USER_CREDENTIALS; ya no
// hay un id de cuenta distinto del id del usuario. Para el cliente delegado
// (bot de Telegram) el id vale el del cliente.
public sealed record AuthenticatedIdentity(
    Guid UserId,
    Guid RoleId,
    string Role,
    string FullName,
    string Email,
    string? PhotoUrl = null);
