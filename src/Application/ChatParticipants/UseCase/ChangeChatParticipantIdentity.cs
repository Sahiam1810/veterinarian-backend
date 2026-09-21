using Application.Common.Abstractions;
using Application.Common.Exceptions;
using MediatR;
using ChatParticipantEntity = Domain.ChatParticipants.Entities.ChatParticipant;

namespace Application.ChatParticipants.UseCase;

public sealed record ChangeChatParticipantIdentityCommand(
    Guid Id,
    Guid? ClientId,
    Guid? AgentHumanId) : IRequest<ChatParticipantEntity>;

public sealed class ChangeChatParticipantIdentityCommandHandler
    : IRequestHandler<ChangeChatParticipantIdentityCommand, ChatParticipantEntity>
{
    private readonly IUnitOfWork _uow;

    public ChangeChatParticipantIdentityCommandHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<ChatParticipantEntity> Handle(
        ChangeChatParticipantIdentityCommand request,
        CancellationToken cancellationToken)
    {
        var participant = await _uow.ChatParticipantsRepository.GetByIdAsync(
            request.Id,
            cancellationToken);
        if (participant is null)
        {
            throw new NotFoundException(
                $"No se encontró el participante '{request.Id}'.");
        }

        if (request.ClientId.HasValue)
        {
            var client = await _uow.ClientsRepository.GetByIdAsync(
                request.ClientId.Value,
                cancellationToken);
            if (client is null)
            {
                throw new NotFoundException(
                    $"No se encontró el cliente '{request.ClientId.Value}'.");
            }
        }

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

        participant.ChangeIdentity(
            request.ClientId,
            request.AgentHumanId);

        await _uow.ChatParticipantsRepository.UpdateAsync(participant, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        return participant;
    }
}
