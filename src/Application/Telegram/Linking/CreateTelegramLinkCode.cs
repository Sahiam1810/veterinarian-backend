using Application.Telegram.Abstractions;
using Application.Telegram.Errors;
using Domain.Telegram.Entities;
using MediatR;

namespace Application.Telegram.Linking;

public sealed record CreateTelegramLinkCodeCommand(Guid PersonId)
    : IRequest<TelegramLinkCodeResult>;

public sealed record TelegramLinkCodeResult(
    string Code,
    string DeepLink,
    DateTimeOffset ExpiresAt);

public sealed class CreateTelegramLinkCodeHandler(
    ITelegramUnitOfWork unitOfWork,
    ITelegramLinkCodeProtector protector,
    ITelegramRuntimeSettings settings,
    TimeProvider timeProvider)
    : IRequestHandler<CreateTelegramLinkCodeCommand, TelegramLinkCodeResult>
{
    public async Task<TelegramLinkCodeResult> Handle(
        CreateTelegramLinkCodeCommand request,
        CancellationToken cancellationToken)
    {
        var user = await unitOfWork.UsersRepository.GetByIdAsync(
            request.PersonId,
            cancellationToken);
        if (user is null || !user.IsActive)
        {
            throw new TelegramAccountUnavailableException();
        }

        var now = timeProvider.GetUtcNow();
        var pending = await unitOfWork.LinkCodesRepository
            .GetPendingByPersonIdAsync(
                request.PersonId,
                now.UtcDateTime,
                cancellationToken);
        foreach (var previous in pending ?? [])
        {
            previous.Invalidate(now.UtcDateTime);
            await unitOfWork.LinkCodesRepository.UpdateAsync(
                previous,
                cancellationToken);
        }

        var protectedCode = protector.Create();
        var expiresAt = now.Add(settings.LinkCodeTtl);
        var code = TelegramLinkCode.Create(
            request.PersonId,
            protectedCode.Hash,
            expiresAt.UtcDateTime,
            now.UtcDateTime);
        await unitOfWork.LinkCodesRepository.AddAsync(code, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new TelegramLinkCodeResult(
            protectedCode.RawCode,
            $"https://t.me/{settings.BotUsername}?start={protectedCode.RawCode}",
            expiresAt);
    }
}
