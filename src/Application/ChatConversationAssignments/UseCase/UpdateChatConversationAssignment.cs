using Application.Common.Abstractions;
using Application.Common.Exceptions;
using MediatR;
using ChatConversationAssignmentEntity = Domain.ChatConversationAssignments.Entities.ChatConversationAssignment;

namespace Application.ChatConversationAssignments.UseCase;

public sealed record UpdateChatConversationAssignmentCommand(
    Guid ChatConversationId,
    Guid? AgentHumanId,
    DateTime? AssignedAt,
    DateTime? UnassignedAt) : IRequest<ChatConversationAssignmentEntity>;

public sealed class UpdateChatConversationAssignmentCommandHandler
    : IRequestHandler<UpdateChatConversationAssignmentCommand, ChatConversationAssignmentEntity>
{
    private readonly IUnitOfWork _uow;

    public UpdateChatConversationAssignmentCommandHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<ChatConversationAssignmentEntity> Handle(
        UpdateChatConversationAssignmentCommand request,
        CancellationToken cancellationToken)
    {
        var assignment = await _uow.ChatConversationAssignmentsRepository.GetByConversationIdAsync(
            request.ChatConversationId,
            cancellationToken)
            ?? throw new NotFoundException(
                $"No se encontró la asignación de la conversación '{request.ChatConversationId}'.");

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

        assignment.Update(request.AgentHumanId, request.AssignedAt, request.UnassignedAt);

        await _uow.ChatConversationAssignmentsRepository.UpdateAsync(assignment, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        return assignment;
    }
}
