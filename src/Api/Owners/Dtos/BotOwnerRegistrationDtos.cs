namespace Api.Owners.Dtos;

// Contrato HTTP bot: proof obligatorio. RequireContactProofs no es aceptado desde el body.
public sealed record RegisterOwnerBotRequest(
    string FullName,
    string Email,
    string IdentificationNumber,
    string PhoneNumber,
    Guid ContactProofSessionId,
    string ContactProof,
    string? Address = null);

// Respuesta pública: ids creados; sin password, hash, OTP, proof ni sesión sensible.
public sealed record RegisterOwnerBotResponse(Guid UserId, Guid ClientId);
