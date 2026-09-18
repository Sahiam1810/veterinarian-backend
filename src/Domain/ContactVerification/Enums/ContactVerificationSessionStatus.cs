namespace Domain.ContactVerification.Enums;

// Ciclo de vida de la sesión de contacto (OTP + proof de un solo uso).
public enum ContactVerificationSessionStatus
{
    AwaitingOtp = 1,
    ProofIssued = 2,
    Consumed = 3,
    Expired = 4,
    Blocked = 5,
    Cancelled = 6
}
