using Application.Common.Abstractions;
using Application.Common.Exceptions;
using MediatR;
using ChatEscalationAssignmentEntity = Domain.ChatEscalationAssignments.Entities.ChatEscalationAssignment;

namespace Application.ChatEscalationAssignments.UseCase;

public sealed record UpdateChatEscalationAssignmentCommand(
    Guid Id,
    Guid AgentHumanId,
    DateTime? AssignedAt) : IRequest<ChatEscalationAssignmentEntity>;

public sealed class UpdateChatEscalationAssignmentCommandHandler
    : IRequestHandler<UpdateChatEscalationAssignmentCommand, ChatEscalationAssignmentEntity>
{
    private readonly IUnitOfWork _uow;

    public UpdateChatEscalationAssignmentCommandHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<ChatEscalationAssignmentEntity> Handle(
        UpdateChatEscalationAssignmentCommand request,
        CancellationToken cancellationToken)
    {
        var assignment = await _uow.ChatEscalationAssignmentsRepository.GetByIdAsync(
            request.Id,
            cancellationToken)
            ?? throw new NotFoundException(
                $"No se encontró la asignación '{request.Id}'.");

        var agent = await _uow.AgentHumansRepository.GetByIdAsync(
            request.AgentHumanId,
            cancellationToken);
        if (agent is null)
        {
            throw new NotFoundException(
                $"No se encontró el agente humano '{request.AgentHumanId}'.");
        }

        assignment.Update(request.AgentHumanId, request.AssignedAt);

        await _uow.ChatEscalationAssignmentsRepository.UpdateAsync(assignment, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        return assignment;
    }
}
