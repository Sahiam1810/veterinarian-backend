using Application.Common.Abstractions;
using Application.Common.Exceptions;
using MediatR;
using ChatEscalationEntity = Domain.ChatEscalations.Entities.ChatEscalation;

namespace Application.ChatEscalations.UseCase;

public sealed record CreateChatEscalationCommand(
    Guid ChatConversationId,
    Guid EscalationStatusId,
    bool FromAi,
    string? Reason,
    string? UpdateAt) : IRequest<ChatEscalationEntity>;

public sealed class CreateChatEscalationCommandHandler
    : IRequestHandler<CreateChatEscalationCommand, ChatEscalationEntity>
{
    private readonly IUnitOfWork _uow;

    public CreateChatEscalationCommandHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<ChatEscalationEntity> Handle(
        CreateChatEscalationCommand request,
        CancellationToken cancellationToken)
    {
        var conversation = await _uow.ChatConversationsRepository.GetByIdAsync(
            request.ChatConversationId,
            cancellationToken);
        if (conversation is null)
        {
            throw new NotFoundException(
                $"No se encontró la conversación '{request.ChatConversationId}'.");
        }

        var status = await _uow.EscalationStatusesRepository.GetByIdAsync(
            request.EscalationStatusId,
            cancellationToken);
        if (status is null)
        {
            throw new NotFoundException(
                $"No se encontró el estado de escalamiento '{request.EscalationStatusId}'.");
        }

        var escalation = ChatEscalationEntity.Create(
            request.ChatConversationId,
            request.EscalationStatusId,
            request.FromAi,
            request.Reason,
            request.UpdateAt);

        await _uow.ChatEscalationsRepository.AddAsync(escalation, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        return escalation;
    }
}
