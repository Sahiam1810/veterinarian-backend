using Application.Common.Abstractions;
using Application.Common.Exceptions;
using Application.Notifications.Abstraction;
using MediatR;
using Microsoft.Extensions.Logging;
using ChatEscalationEntity = Domain.ChatEscalations.Entities.ChatEscalation;

namespace Application.ChatEscalations.UseCase;

public sealed record ResolveChatEscalationCommand(
    Guid Id,
    Guid EscalationStatusId,
    Guid ResolvedBy,
    string? ResolutionNote) : IRequest<ChatEscalationEntity>;

public sealed class ResolveChatEscalationCommandHandler(
    IUnitOfWork uow,
    IChatRealtimeNotifier notifier,
    ILogger<ResolveChatEscalationCommandHandler> logger)
    : IRequestHandler<ResolveChatEscalationCommand, ChatEscalationEntity>
{
    public async Task<ChatEscalationEntity> Handle(
        ResolveChatEscalationCommand request,
        CancellationToken cancellationToken)
    {
        var escalation = await uow.ChatEscalationsRepository.GetByIdAsync(
            request.Id,
            cancellationToken);
        if (escalation is null)
        {
            throw new NotFoundException(
                $"No se encontró el escalamiento '{request.Id}'.");
        }

        var status = await uow.EscalationStatusesRepository.GetByIdAsync(
            request.EscalationStatusId,
            cancellationToken);
        if (status is null)
        {
            throw new NotFoundException(
                $"No se encontró el estado de escalamiento '{request.EscalationStatusId}'.");
        }

        escalation.Resolve(
            request.EscalationStatusId,
            request.ResolvedBy,
            request.ResolutionNote);

        await uow.ChatEscalationsRepository.UpdateAsync(escalation, cancellationToken);
        await uow.SaveChangesAsync(cancellationToken);

        try
        {
            await notifier.NotifyEscalationResolvedAsync(
                new ChatEscalationResolvedPayload(
                    escalation.Id,
                    escalation.ChatConversationId,
                    escalation.ResolvedBy?.ToString(),
                    escalation.ResolvedAt ?? DateTime.UtcNow,
                    escalation.ResolutionNote),
                cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Error al notificar la resolución del escalamiento {EscalationId}", escalation.Id);
        }

        return escalation;
    }
}
