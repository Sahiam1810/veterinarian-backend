using Application.Telegram.Abstractions;
using Application.Telegram.Processing;
using MediatR;

namespace Infrastructure.Telegram.Workers;

public sealed class TelegramUpdatePump(
    ITelegramUnitOfWork unitOfWork,
    ISender sender,
    ITelegramRuntimeSettings settings,
    TimeProvider timeProvider)
{
    public async Task<bool> RunOnceAsync(CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var update = await unitOfWork.InboundUpdatesRepository.ClaimNextAsync(
            now,
            now.Subtract(settings.ProcessingLease),
            cancellationToken);
        if (update is null)
        {
            return false;
        }

        await sender.Send(new ProcessTelegramUpdateCommand(update.Id), cancellationToken);
        return true;
    }
}
