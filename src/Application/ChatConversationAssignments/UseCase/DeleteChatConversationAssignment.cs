using Application.Common.Abstractions;
using Application.Common.Exceptions;
using MediatR;

namespace Application.ChatConversationAssignments.UseCase;

public sealed record DeleteChatConversationAssignmentCommand(Guid ChatConversationId) : IRequest;

public sealed class DeleteChatConversationAssignmentCommandHandler
    : IRequestHandler<DeleteChatConversationAssignmentCommand>
{
    private readonly IUnitOfWork _uow;

    public DeleteChatConversationAssignmentCommandHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task Handle(
        DeleteChatConversationAssignmentCommand request,
        CancellationToken cancellationToken)
    {
        var assignment = await _uow.ChatConversationAssignmentsRepository.GetByConversationIdAsync(
            request.ChatConversationId,
            cancellationToken)
            ?? throw new NotFoundException(
                $"No se encontró la asignación de la conversación '{request.ChatConversationId}'.");

        await _uow.ChatConversationAssignmentsRepository.DeleteAsync(assignment, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);
    }
}
