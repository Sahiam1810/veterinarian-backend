using Application.Common.Abstractions;
using Application.Common.Exceptions;
using MediatR;

namespace Application.ChatEscalationAssignments.UseCase;

public sealed record DeleteChatEscalationAssignmentCommand(Guid Id) : IRequest;

public sealed class DeleteChatEscalationAssignmentCommandHandler
    : IRequestHandler<DeleteChatEscalationAssignmentCommand>
{
    private readonly IUnitOfWork _uow;

    public DeleteChatEscalationAssignmentCommandHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task Handle(
        DeleteChatEscalationAssignmentCommand request,
        CancellationToken cancellationToken)
    {
        var assignment = await _uow.ChatEscalationAssignmentsRepository.GetByIdAsync(
            request.Id,
            cancellationToken)
            ?? throw new NotFoundException(
                $"No se encontró la asignación '{request.Id}'.");

        await _uow.ChatEscalationAssignmentsRepository.DeleteAsync(assignment, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);
    }
}
