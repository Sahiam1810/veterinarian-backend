using Application.Telegram.Models;

namespace Application.Telegram.Abstractions;

public interface ITelegramClientIdentityGateway
{
    Task<TelegramClientIdentity?> FindActiveByIdentificationAsync(
        string identificationNumber,
        CancellationToken cancellationToken);

    Task<TelegramClientIdentity?> FindActiveByPersonIdAsync(
        Guid personId,
        CancellationToken cancellationToken);

    Task<TelegramClientIdentity> StageRegistrationAsync(
        TelegramClientRegistration registration,
        CancellationToken cancellationToken);
}
