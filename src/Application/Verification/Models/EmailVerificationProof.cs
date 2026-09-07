namespace Application.Verification.Models;

// Comprobante emitido tras la confirmación exitosa de un código OTP enviado por email.
public sealed record EmailVerificationProof(
    string Email,
    string ProofToken,
    DateTime VerifiedAtUtc,
    DateTime ExpiresAtUtc);
