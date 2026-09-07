using Application.Owners.Abstractions;

namespace Application.Owners.Abstractions;

public sealed record RegisterOwnerFromTelegramRequest(
    string FullName,
    string Email,
    string IdentificationNumber,
    string PhoneNumber,
    Guid ContactProofSessionId,
    string ContactProof,
    long TelegramUserId,
    string? Address = null);

public interface IRegisterOwnerFromTelegram
{
    Task<RegisterOwnerResult> RegisterAsync(
        RegisterOwnerFromTelegramRequest request,
        CancellationToken cancellationToken);
}
