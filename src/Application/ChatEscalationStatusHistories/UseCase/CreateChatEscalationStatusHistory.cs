using Application.Common.Abstractions;
using Application.Common.Exceptions;
using MediatR;
using ChatEscalationStatusHistoryEntity = Domain.ChatEscalationStatusHistories.Entities.ChatEscalationStatusHistory;

namespace Application.ChatEscalationStatusHistories.UseCase;

public sealed record CreateChatEscalationStatusHistoryCommand(
    Guid EscalationStatusId,
    Guid ChatEscalationId) : IRequest<ChatEscalationStatusHistoryEntity>;

public sealed class CreateChatEscalationStatusHistoryCommandHandler
    : IRequestHandler<CreateChatEscalationStatusHistoryCommand, ChatEscalationStatusHistoryEntity>
{
    private readonly IUnitOfWork _uow;

    public CreateChatEscalationStatusHistoryCommandHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<ChatEscalationStatusHistoryEntity> Handle(
        CreateChatEscalationStatusHistoryCommand request,
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

        var status = await _uow.EscalationStatusesRepository.GetByIdAsync(
            request.EscalationStatusId,
            cancellationToken);
        if (status is null)
        {
            throw new NotFoundException(
                $"No se encontró el estado de escalamiento '{request.EscalationStatusId}'.");
        }

        var history = ChatEscalationStatusHistoryEntity.Create(
            request.EscalationStatusId,
            request.ChatEscalationId);

        await _uow.ChatEscalationStatusHistoriesRepository.AddAsync(history, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        return history;
    }
}
