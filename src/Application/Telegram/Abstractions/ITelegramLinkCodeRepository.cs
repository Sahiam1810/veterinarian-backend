using Domain.Telegram.Entities;

namespace Application.Telegram.Abstractions;

public interface ITelegramLinkCodeRepository
{
    Task<TelegramLinkCode?> GetActiveByHashAsync(
        string codeHash,
        DateTime now,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<TelegramLinkCode>> GetPendingByPersonIdAsync(
        Guid personId,
        DateTime now,
        CancellationToken cancellationToken);

    Task AddAsync(TelegramLinkCode code, CancellationToken cancellationToken);

    Task UpdateAsync(TelegramLinkCode code, CancellationToken cancellationToken);
}
