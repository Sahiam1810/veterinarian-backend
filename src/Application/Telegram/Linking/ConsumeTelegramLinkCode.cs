using Application.Telegram.Abstractions;
using Application.Telegram.Errors;
using Domain.Telegram.Entities;
using MediatR;

namespace Application.Telegram.Linking;

public sealed record ConsumeTelegramLinkCodeCommand(
    string Code,
    long TelegramUserId,
    long TelegramChatId) : IRequest<Guid>;

public sealed class ConsumeTelegramLinkCodeHandler(
    ITelegramUnitOfWork unitOfWork,
    ITelegramLinkCodeProtector protector,
    TimeProvider timeProvider)
    : IRequestHandler<ConsumeTelegramLinkCodeCommand, Guid>
{
    public async Task<Guid> Handle(
        ConsumeTelegramLinkCodeCommand request,
        CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var hash = protector.Hash(request.Code);
        var code = await unitOfWork.LinkCodesRepository
            .GetActiveByHashAsync(hash, now, cancellationToken)
            ?? throw new TelegramLinkCodeInvalidException();
        var user = await unitOfWork.UsersRepository.GetByIdAsync(
            code.PersonId,
            cancellationToken);
        if (user is null || !user.IsActive)
        {
            throw new TelegramAccountUnavailableException();
        }

        var externalLink = await unitOfWork.UserLinksRepository
            .GetByTelegramUserIdAsync(request.TelegramUserId, cancellationToken);
        if (externalLink is not null && externalLink.PersonId != code.PersonId)
        {
            throw new TelegramIdentityConflictException();
        }

        var link = externalLink ?? await unitOfWork.UserLinksRepository
            .GetByPersonIdAsync(code.PersonId, cancellationToken);
        if (link is null)
        {
            link = TelegramUserLink.Create(
                code.PersonId,
                request.TelegramUserId,
                request.TelegramChatId,
                now);
            await unitOfWork.UserLinksRepository.AddAsync(
                link,
                cancellationToken);
        }
        else
        {
            link.Relink(request.TelegramUserId, request.TelegramChatId, now);
            await unitOfWork.UserLinksRepository.UpdateAsync(
                link,
                cancellationToken);
        }

        code.Consume(now);
        await unitOfWork.LinkCodesRepository.UpdateAsync(code, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return link.Id;
    }
}
