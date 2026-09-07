namespace Api.ContactVerification.Dtos;

// Esqueleto v1: solo email. Purpose = Register | Claim.
public sealed record RequestContactEmailVerificationRequest(
    string Email,
    string Purpose,
    Guid? SubjectUserId = null);

public sealed record RequestContactEmailVerificationResponse(Guid SessionId);

public sealed record ConfirmContactEmailVerificationRequest(
    Guid SessionId,
    string Code);

public sealed record ConfirmContactEmailVerificationResponse(
    Guid SessionId,
    string Proof);
