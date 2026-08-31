using Application.Common.Abstractions;
using Application.Common.Exceptions;
using MediatR;

namespace Application.ChatEscalationStatusHistories.UseCase;

public sealed record DeleteChatEscalationStatusHistoryCommand(Guid Id) : IRequest;

public sealed class DeleteChatEscalationStatusHistoryCommandHandler
    : IRequestHandler<DeleteChatEscalationStatusHistoryCommand>
{
    private readonly IUnitOfWork _uow;

    public DeleteChatEscalationStatusHistoryCommandHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task Handle(
        DeleteChatEscalationStatusHistoryCommand request,
        CancellationToken cancellationToken)
    {
        var history = await _uow.ChatEscalationStatusHistoriesRepository.GetByIdAsync(
            request.Id,
            cancellationToken)
            ?? throw new NotFoundException(
                $"No se encontró el historial de estado '{request.Id}'.");

        await _uow.ChatEscalationStatusHistoriesRepository.DeleteAsync(history, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);
    }
}
