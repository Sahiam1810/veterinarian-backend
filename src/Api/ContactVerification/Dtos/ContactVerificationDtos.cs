namespace Api.ContactVerification.Dtos;

// v1: solo email. Purpose = Register | Claim.
public sealed record RequestContactEmailVerificationRequest(
    string Email,
    string Purpose,
    Guid? SubjectUserId = null);

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
