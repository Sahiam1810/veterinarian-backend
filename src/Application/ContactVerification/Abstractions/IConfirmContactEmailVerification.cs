namespace Application.ContactVerification.Abstractions;

// Confirma el OTP de correo y emite proof de un solo uso (3.2).
public sealed record ConfirmContactEmailVerification(
    Guid SessionId,
    string Code);

public sealed record ConfirmContactEmailVerificationResult(
    Guid SessionId,
    string Proof);

public interface IConfirmContactEmailVerification
{
    Task<ConfirmContactEmailVerificationResult> ConfirmAsync(
        ConfirmContactEmailVerification request,
        CancellationToken cancellationToken);
}
