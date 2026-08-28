using Application.Common.Abstractions;
using Application.Common.Exceptions;
using MediatR;
using ChatConversationAssignmentEntity = Domain.ChatConversationAssignments.Entities.ChatConversationAssignment;

namespace Application.ChatConversationAssignments.UseCase;

public sealed record CreateChatConversationAssignmentCommand(
    Guid ChatConversationId,
    Guid? AgentHumanId,
    DateTime? AssignedAt) : IRequest<ChatConversationAssignmentEntity>;

public sealed class CreateChatConversationAssignmentCommandHandler
    : IRequestHandler<CreateChatConversationAssignmentCommand, ChatConversationAssignmentEntity>
{
    private readonly IUnitOfWork _uow;

    public CreateChatConversationAssignmentCommandHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<ChatConversationAssignmentEntity> Handle(
        CreateChatConversationAssignmentCommand request,
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

        if (await _uow.ChatConversationAssignmentsRepository.ExistsByConversationIdAsync(
                request.ChatConversationId,
                cancellationToken))
        {
            throw new ArgumentException(
                $"Ya existe una asignación para la conversación '{request.ChatConversationId}'.");
        }

        if (request.AgentHumanId.HasValue)
        {
            var agent = await _uow.AgentHumansRepository.GetByIdAsync(
                request.AgentHumanId.Value,
                cancellationToken);
            if (agent is null)
            {
                throw new NotFoundException(
                    $"No se encontró el agente humano '{request.AgentHumanId.Value}'.");
            }
        }

        var assignment = ChatConversationAssignmentEntity.Create(
            request.ChatConversationId,
            request.AgentHumanId,
            request.AssignedAt);

        await _uow.ChatConversationAssignmentsRepository.AddAsync(assignment, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        return assignment;
    }
}
