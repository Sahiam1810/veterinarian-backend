using Application.Common.Abstractions;
using Application.Common.Exceptions;
using MediatR;
using ChatEscalationAssignmentEntity = Domain.ChatEscalationAssignments.Entities.ChatEscalationAssignment;

namespace Application.ChatEscalationAssignments.UseCase;

public sealed record CreateChatEscalationAssignmentCommand(
    Guid AgentHumanId,
    Guid ChatEscalationId,
    DateTime? AssignedAt) : IRequest<ChatEscalationAssignmentEntity>;

public sealed class CreateChatEscalationAssignmentCommandHandler
    : IRequestHandler<CreateChatEscalationAssignmentCommand, ChatEscalationAssignmentEntity>
{
    private readonly IUnitOfWork _uow;

    public CreateChatEscalationAssignmentCommandHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<ChatEscalationAssignmentEntity> Handle(
        CreateChatEscalationAssignmentCommand request,
        CancellationToken cancellationToken)
    {
        var escalation = await _uow.ChatEscalationsRepository.GetByIdAsync(
            request.ChatEscalationId,
            cancellationToken);
        if (escalation is null)
        {
            throw new NotFoundException(
                $"No se encontró el escalamiento '{request.ChatEscalationId}'.");
        }

        var agent = await _uow.AgentHumansRepository.GetByIdAsync(
            request.AgentHumanId,
            cancellationToken);
        if (agent is null)
        {
            throw new NotFoundException(
                $"No se encontró el agente humano '{request.AgentHumanId}'.");
        }

        var assignment = ChatEscalationAssignmentEntity.Create(
            request.AgentHumanId,
            request.ChatEscalationId,
            request.AssignedAt);

        await _uow.ChatEscalationAssignmentsRepository.AddAsync(assignment, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        return assignment;
    }
}
