using Application.Common.Abstractions;
using Application.Common.Exceptions;
using MediatR;
using ChatParticipantEntity = Domain.ChatParticipants.Entities.ChatParticipant;

namespace Application.ChatParticipants.UseCase;

public sealed record CreateChatParticipantCommand(
    Guid ChatConversationId,
    Guid ParticipantTypeId,
    Guid? ClientId,
    Guid? AgentHumanId) : IRequest<ChatParticipantEntity>;

public sealed class CreateChatParticipantCommandHandler
    : IRequestHandler<CreateChatParticipantCommand, ChatParticipantEntity>
{
    private readonly IUnitOfWork _uow;

    public CreateChatParticipantCommandHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<ChatParticipantEntity> Handle(
        CreateChatParticipantCommand request,
        CancellationToken cancellationToken)
    {
        var conversation = await _uow.ChatConversationsRepository.GetByIdAsync(
            request.ChatConversationId,
            cancellationToken);
        if (conversation is null)
        {
            throw new NotFoundException(
                $"No se encontró la conversación '{request.ChatConversationId}'.");
        }

        var participantType = await _uow.SenderTypesRepository.GetByIdAsync(
            request.ParticipantTypeId,
            cancellationToken);
        if (participantType is null)
        {
            throw new NotFoundException(
                $"No se encontró el tipo de participante '{request.ParticipantTypeId}'.");
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

        var participant = ChatParticipantEntity.Create(
            request.ChatConversationId,
            request.ParticipantTypeId,
            request.ClientId,
            request.AgentHumanId);

        await _uow.ChatParticipantsRepository.AddAsync(participant, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        return participant;
    }
}
