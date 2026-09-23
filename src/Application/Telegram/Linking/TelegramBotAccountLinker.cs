using Application.Telegram.Abstractions;
using Application.Telegram.Errors;
using Domain.Telegram.Entities;

namespace Application.Telegram.Linking;

public sealed record TelegramBotAccountLinkResult(Guid LinkId, string FullName);

public sealed class TelegramBotAccountLinker(
    ITelegramUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    public async Task<TelegramBotAccountLinkResult> LinkAsync(
        Guid clientId,
        long telegramUserId,
        bool requireRecentRegistration,
        TimeSpan registrationLinkWindow,
        CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var client = await unitOfWork.ClientsRepository.GetByIdAsync(clientId, cancellationToken);
        if (client is null || !client.IsActive)
        {
            throw new TelegramAccountUnavailableException();
        }

        var existingTelegramLink = await unitOfWork.UserLinksRepository
            .GetByTelegramUserIdAsync(telegramUserId, cancellationToken);
        if (existingTelegramLink is not null && existingTelegramLink.ClientId != clientId)
        {
            if (requireRecentRegistration)
            {
                throw new TelegramClientLinkRequiresProofException();
            }

            throw new TelegramIdentityConflictException();
        }

        if (existingTelegramLink is not null)
        {
            return new TelegramBotAccountLinkResult(existingTelegramLink.Id, client.FullName.Value);
        }

        var existingClientLink = await unitOfWork.UserLinksRepository
            .GetByClientIdAsync(clientId, cancellationToken);
        if (existingClientLink is not null && requireRecentRegistration)
        {
            throw new TelegramClientLinkRequiresProofException();
        }

        if (requireRecentRegistration && now - client.CreatedAt >= registrationLinkWindow)
        {
            throw new TelegramClientLinkRequiresProofException();
        }

        var link = existingClientLink;
        if (link is null)
        {
            link = TelegramUserLink.Create(
                clientId,
                telegramUserId,
                telegramUserId,
                now);
            await unitOfWork.UserLinksRepository.AddAsync(link, cancellationToken);
        }
        else if (link.TelegramUserId != telegramUserId ||
                 link.TelegramChatId != telegramUserId)
        {
            link.Relink(telegramUserId, telegramUserId, now);
            await unitOfWork.UserLinksRepository.UpdateAsync(link, cancellationToken);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return new TelegramBotAccountLinkResult(link.Id, client.FullName.Value);
    }
}