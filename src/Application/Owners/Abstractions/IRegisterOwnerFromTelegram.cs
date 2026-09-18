using Application.Owners.Abstractions;

namespace Application.Owners.Abstractions;

// TelegramUserId es para el enlace posterior; el núcleo no crea login.
// ContactProof es opcional: la equivalencia ADR es OTP Gmail de la sesión Telegram.
public sealed record RegisterOwnerFromTelegramRequest(
    string FullName,
    string Email,
    string IdentificationNumber,
    string PhoneNumber,
    long TelegramUserId,
    string? Address = null,
    Guid? ContactProofSessionId = null,
    string? ContactProof = null);

public interface IRegisterOwnerFromTelegram
{
    Task<RegisterOwnerResult> RegisterAsync(
        RegisterOwnerFromTelegramRequest request,
        CancellationToken cancellationToken);
}
