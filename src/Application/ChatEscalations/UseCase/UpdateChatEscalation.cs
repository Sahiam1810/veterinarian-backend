using Application.Common.Abstractions;
using Application.Common.Exceptions;
using MediatR;
using ChatEscalationEntity = Domain.ChatEscalations.Entities.ChatEscalation;

namespace Application.ChatEscalations.UseCase;

public sealed record UpdateChatEscalationCommand(
    Guid Id,
    Guid EscalationStatusId,
    bool FromAi,
    string? Reason,
    string? UpdateAt) : IRequest<ChatEscalationEntity>;

public sealed class UpdateChatEscalationCommandHandler
    : IRequestHandler<UpdateChatEscalationCommand, ChatEscalationEntity>
{
    private readonly IUnitOfWork _uow;

    public UpdateChatEscalationCommandHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<ChatEscalationEntity> Handle(
        UpdateChatEscalationCommand request,
        CancellationToken cancellationToken)
    {
        var escalation = await _uow.ChatEscalationsRepository.GetByIdAsync(
            request.Id,
            cancellationToken)
            ?? throw new NotFoundException(
                $"No se encontró el escalamiento '{request.Id}'.");

        var status = await _uow.EscalationStatusesRepository.GetByIdAsync(
            request.EscalationStatusId,
            cancellationToken);
        if (status is null)
        {
            throw new NotFoundException(
                $"No se encontró el estado de escalamiento '{request.EscalationStatusId}'.");
        }

        escalation.Update(
            request.EscalationStatusId,
            request.FromAi,
            request.Reason,
            request.UpdateAt);

        await _uow.ChatEscalationsRepository.UpdateAsync(escalation, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        return escalation;
    }
}
