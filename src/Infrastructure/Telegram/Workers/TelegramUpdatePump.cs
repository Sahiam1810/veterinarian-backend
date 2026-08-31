using Application.Telegram.Abstractions;
using Application.Telegram.Processing;
using MediatR;

namespace Infrastructure.Telegram.Workers;

public sealed class TelegramUpdatePump(
    ITelegramUnitOfWork unitOfWork,
    ISender sender,
    TimeProvider timeProvider)
{
    public async Task<bool> RunOnceAsync(CancellationToken cancellationToken)
    {
        var update = await unitOfWork.InboundUpdatesRepository.ClaimNextAsync(
            timeProvider.GetUtcNow().UtcDateTime,
            cancellationToken);
        if (update is null)
        {
            return false;
        }

        await sender.Send(new ProcessTelegramUpdateCommand(update.Id), cancellationToken);
        return true;
    }
}
