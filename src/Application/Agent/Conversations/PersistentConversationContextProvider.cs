using Application.Agent.Abstractions;
using Application.Agent.Errors;
using Application.Agent.Messages;
using Application.Common.Abstractions;
using Domain.ChatConversations.Entities;
using Domain.ChatParticipants.Entities;

namespace Application.Agent.Conversations;

public sealed class PersistentConversationContextProvider(
    IUnitOfWork unitOfWork,
    IAgentConversationDefaults defaults,
    IActiveConversationEscalationReader escalationReader) : IConversationContextProvider
{
    public async ValueTask<AgentConversationContext> ResolveAsync(
        Guid personId,
        Guid? requestedConversationId,
        string idempotencyKey,
        string channel,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _ = idempotencyKey;

        _ = await unitOfWork.ClientsRepository.GetByIdAsync(personId, cancellationToken)
            ?? throw new AgentConversationForbiddenException();

        if (requestedConversationId is { } conversationId)
        {
            return await ResolveExistingAsync(personId, conversationId, cancellationToken);
        }

        await EnsureCatalogsExistAsync(cancellationToken);

        var conversation = ChatConversation.Create(channel: channel);
        var participant = ChatParticipant.Create(
            conversation.Id,
            defaults.ClientParticipantTypeId,
            clientId: personId);

        await unitOfWork.ChatConversationsRepository.AddAsync(
            conversation,
            cancellationToken);
        await unitOfWork.ChatParticipantsRepository.AddAsync(
            participant,
            cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new AgentConversationContext(conversation.Id, "web", false);
    }

    private async ValueTask<AgentConversationContext> ResolveExistingAsync(
        Guid personId,
        Guid conversationId,
        CancellationToken cancellationToken)
    {
        _ = await unitOfWork.ChatConversationsRepository.GetByIdAsync(
                conversationId,
                cancellationToken)
            ?? throw new AgentConversationNotFoundException();
        var participants = await unitOfWork.ChatParticipantsRepository
            .GetAllByConversationIdAsync(conversationId, cancellationToken);
        var isParticipant = participants.Any(participant => participant.ClientId == personId);
        if (!isParticipant)
        {
            throw new AgentConversationForbiddenException();
        }

        var isEscalated = await escalationReader.HasActiveAsync(
            conversationId,
            cancellationToken);
        return new AgentConversationContext(conversationId, "web", isEscalated);
    }

    private async Task EnsureCatalogsExistAsync(CancellationToken cancellationToken)
    {
        var participantType = await unitOfWork.SenderTypesRepository.GetByIdAsync(
            defaults.ClientParticipantTypeId,
            cancellationToken);
        if (participantType is null)
        {
            throw new AgentConversationConfigurationException();
        }
    }
}
