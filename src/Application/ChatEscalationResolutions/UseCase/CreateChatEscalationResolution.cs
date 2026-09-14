using Application.Common.Abstractions;
using Application.Common.Exceptions;
using Application.Notifications.Abstraction;
using MediatR;
using Microsoft.Extensions.Logging;
using ChatEscalationResolutionEntity = Domain.ChatEscalationResolutions.Entities.ChatEscalationResolution;

namespace Application.ChatEscalationResolutions.UseCase;

public sealed record CreateChatEscalationResolutionCommand(
    Guid ChatEscalationId,
    Guid? ResolvedBy,
    string? ResolutionNote,
    DateTime? ResolvedAt) : IRequest<ChatEscalationResolutionEntity>;

public sealed class CreateChatEscalationResolutionCommandHandler(
    IUnitOfWork uow,
    IChatRealtimeNotifier chatRealtimeNotifier,
    ILogger<CreateChatEscalationResolutionCommandHandler> logger)
    : IRequestHandler<CreateChatEscalationResolutionCommand, ChatEscalationResolutionEntity>
{
    public async Task<ChatEscalationResolutionEntity> Handle(
        CreateChatEscalationResolutionCommand request,
        CancellationToken cancellationToken)
    {
        var escalation = await uow.ChatEscalationsRepository.GetByIdAsync(
            request.ChatEscalationId,
            cancellationToken);
        if (escalation is null)
        {
            throw new NotFoundException(
                $"No se encontró el escalamiento '{request.ChatEscalationId}'.");
        }

        var resolution = ChatEscalationResolutionEntity.Create(
            request.ChatEscalationId,
            request.ResolvedBy,
            request.ResolutionNote,
            request.ResolvedAt);

        await uow.ChatEscalationResolutionsRepository.AddAsync(resolution, cancellationToken);
        await uow.SaveChangesAsync(cancellationToken);

        // Ticket B5: ver el mismo razonamiento en CreateChatEscalationCommandHandler
        // — un fallo de broadcast nunca debe tumbar la resolución ya guardada.
        try
        {
            var payload = new ChatEscalationResolvedPayload(
                resolution.ChatEscalationId,
                escalation.ChatConversationId,
                resolution.ResolvedBy?.ToString(),
                resolution.ResolvedAt ?? DateTime.UtcNow,
                resolution.ResolutionNote);
            await chatRealtimeNotifier.NotifyEscalationResolvedAsync(payload, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(
                exception,
                "Failed to broadcast ChatEscalationResolved for escalation {EscalationId}.",
                resolution.ChatEscalationId);
        }

        return resolution;
    }
}
