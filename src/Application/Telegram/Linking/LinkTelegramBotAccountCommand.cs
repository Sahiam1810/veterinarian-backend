using Application.Telegram.Abstractions;
using Application.Telegram.Errors;
using Domain.Telegram.Entities;
using MediatR;

namespace Application.Telegram.Linking;

// Vincula el TelegramUserId real con el cliente registrado o encontrado por el bot.
public sealed record LinkTelegramBotAccountCommand(
    Guid ClientId,
    long TelegramUserId) : IRequest<Guid>;

public sealed class LinkTelegramBotAccountHandler(
    ITelegramUnitOfWork unitOfWork,
    TimeProvider timeProvider)
    : IRequestHandler<LinkTelegramBotAccountCommand, Guid>
{
    public async Task<Guid> Handle(
        LinkTelegramBotAccountCommand request,
        CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;

        var client = await unitOfWork.ClientsRepository.GetByIdAsync(
            request.ClientId,
            cancellationToken);
        if (client is null || !client.IsActive)
        {
            throw new TelegramAccountUnavailableException();
        }

        var existingLink = await unitOfWork.UserLinksRepository
            .GetByTelegramUserIdAsync(request.TelegramUserId, cancellationToken);
        if (existingLink is not null && existingLink.ClientId != request.ClientId)
        {
            throw new TelegramIdentityConflictException();
        }

        var link = existingLink ?? await unitOfWork.UserLinksRepository
            .GetByClientIdAsync(request.ClientId, cancellationToken);
        if (link is null)
        {
            link = TelegramUserLink.Create(
                request.ClientId,
                request.TelegramUserId,
                request.TelegramUserId,
                now);
            await unitOfWork.UserLinksRepository.AddAsync(link, cancellationToken);
        }
        else if (link.TelegramUserId != request.TelegramUserId ||
                  link.TelegramChatId != request.TelegramUserId)
        {
            link.Relink(request.TelegramUserId, request.TelegramUserId, now);
            await unitOfWork.UserLinksRepository.UpdateAsync(link, cancellationToken);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return link.Id;
    }
}
