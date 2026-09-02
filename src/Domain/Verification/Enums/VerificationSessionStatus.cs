namespace Domain.Verification.Enums;

// Estados del ciclo de vida de una sesión OTP genérica.
public enum VerificationSessionStatus
{
    AwaitingOtp = 1,
    Completed = 2,
    Expired = 3,
    Blocked = 4,
    Cancelled = 5
}
