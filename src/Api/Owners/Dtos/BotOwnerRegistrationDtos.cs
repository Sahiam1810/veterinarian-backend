namespace Api.Owners.Dtos;

// Contrato HTTP bot: sin proof ni código de verificación (decisión de negocio).
public sealed record RegisterOwnerBotRequest(
    string FullName,
    string Email,
    string IdentificationNumber,
    string PhoneNumber,
    string? Address = null);

// Respuesta pública: ids creados; sin password, hash, OTP, proof ni sesión sensible.
public sealed record RegisterOwnerBotResponse(Guid ClientId);
