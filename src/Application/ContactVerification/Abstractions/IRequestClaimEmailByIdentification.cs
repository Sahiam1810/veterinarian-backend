using Domain.ContactVerification.Enums;

namespace Application.ContactVerification.Abstractions;

public sealed record RequestClaimEmailByIdentification(string IdentificationNumber);

public sealed record RequestClaimEmailByIdentificationResult(
    Guid SessionId,
    DateTime ExpiresAt,
    ContactVerificationChannel Channel,
    string MaskedEmail);

public interface IRequestClaimEmailByIdentification
{
    Task<RequestClaimEmailByIdentificationResult> RequestAsync(
        RequestClaimEmailByIdentification request,
        CancellationToken cancellationToken);
}
