namespace Api.ContactVerification.Dtos;

// v1: solo email. Purpose = Register.
public sealed record RequestContactEmailVerificationRequest(
    string Email,
    string Purpose);

// Metadatos seguros de sesión; nunca incluye el OTP.
public sealed record RequestContactEmailVerificationResponse(
    Guid SessionId,
    DateTime ExpiresAt,
    string Channel);

public sealed record ConfirmContactEmailVerificationRequest(
    Guid SessionId,
    string Code);

public sealed record ConfirmContactEmailVerificationResponse(
    Guid SessionId,
    string Proof);

public sealed record RequestClaimEmailByIdentificationRequest(string IdentificationNumber);

public sealed record RequestClaimEmailByIdentificationResponse(
    Guid SessionId,
    DateTime ExpiresAt,
    string Channel,
    string MaskedEmail);

