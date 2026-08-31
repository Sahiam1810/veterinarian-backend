using Application.Common.Abstractions;
using Application.Common.Exceptions;
using MediatR;

namespace Application.ChatEscalations.UseCase;

public sealed record DeleteChatEscalationCommand(Guid Id) : IRequest;

public sealed class DeleteChatEscalationCommandHandler
    : IRequestHandler<DeleteChatEscalationCommand>
{
    private readonly IUnitOfWork _uow;

    public DeleteChatEscalationCommandHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task Handle(
        DeleteChatEscalationCommand request,
        CancellationToken cancellationToken)
    {
        var escalation = await _uow.ChatEscalationsRepository.GetByIdAsync(
            request.Id,
            cancellationToken)
            ?? throw new NotFoundException(
                $"No se encontró el escalamiento '{request.Id}'.");

        await _uow.ChatEscalationsRepository.DeleteAsync(escalation, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);
    }
}
