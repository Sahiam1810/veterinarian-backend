using Application.Common.Abstractions;
using Application.Common.Exceptions;
using MediatR;
using ChatEscalationStatusHistoryEntity = Domain.ChatEscalationStatusHistories.Entities.ChatEscalationStatusHistory;

namespace Application.ChatEscalationStatusHistories.UseCase;

public sealed record UpdateChatEscalationStatusHistoryCommand(
    Guid Id,
    Guid EscalationStatusId) : IRequest<ChatEscalationStatusHistoryEntity>;

public sealed class UpdateChatEscalationStatusHistoryCommandHandler
    : IRequestHandler<UpdateChatEscalationStatusHistoryCommand, ChatEscalationStatusHistoryEntity>
{
    private readonly IUnitOfWork _uow;

    public UpdateChatEscalationStatusHistoryCommandHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<ChatEscalationStatusHistoryEntity> Handle(
        UpdateChatEscalationStatusHistoryCommand request,
        CancellationToken cancellationToken)
    {
        var history = await _uow.ChatEscalationStatusHistoriesRepository.GetByIdAsync(
            request.Id,
            cancellationToken)
            ?? throw new NotFoundException(
                $"No se encontró el historial de estado '{request.Id}'.");

        var status = await _uow.EscalationStatusesRepository.GetByIdAsync(
            request.EscalationStatusId,
            cancellationToken);
        if (status is null)
        {
            throw new NotFoundException(
                $"No se encontró el estado de escalamiento '{request.EscalationStatusId}'.");
        }

        history.Update(request.EscalationStatusId);

        await _uow.ChatEscalationStatusHistoriesRepository.UpdateAsync(history, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        return history;
    }
}
