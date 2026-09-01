using Application.Telegram.Abstractions;
using Domain.Telegram.Entities;
using MediatR;

namespace Application.Telegram.Updates;

public sealed record IngestTelegramUpdateCommand(
    long UpdateId,
    long TelegramUserId,
    long TelegramChatId,
    long TelegramMessageId,
    string ChatType,
    string? Text) : IRequest<IngestTelegramUpdateResult>;

public enum IngestTelegramUpdateResult
{
    Accepted,
    Duplicate
}

public sealed class IngestTelegramUpdateHandler(
    ITelegramUnitOfWork unitOfWork,
    ITelegramUpdateSignal updateSignal,
    TimeProvider timeProvider)
    : IRequestHandler<IngestTelegramUpdateCommand, IngestTelegramUpdateResult>
{
    public async Task<IngestTelegramUpdateResult> Handle(
        IngestTelegramUpdateCommand request,
        CancellationToken cancellationToken)
    {
        if (await unitOfWork.InboundUpdatesRepository.ExistsAsync(
                request.UpdateId,
                cancellationToken))
        {
            return IngestTelegramUpdateResult.Duplicate;
        }

        var update = TelegramInboundUpdate.Create(
            request.UpdateId,
            request.TelegramUserId,
            request.TelegramChatId,
            request.TelegramMessageId,
            request.ChatType,
            request.Text,
            timeProvider.GetUtcNow().UtcDateTime);
        await unitOfWork.InboundUpdatesRepository.AddAsync(
            update,
            cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        updateSignal.Notify();
        return IngestTelegramUpdateResult.Accepted;
    }
}
