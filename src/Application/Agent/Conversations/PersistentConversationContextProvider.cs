using Application.Agent.Abstractions;
using Application.Agent.Errors;
using Application.Agent.Messages;
using Application.Common.Abstractions;
using Domain.ChatConversations.Entities;
using Domain.ChatParticipants.Entities;
using Domain.ChatUserProfiles.Entities;

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
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _ = idempotencyKey;

        _ = await unitOfWork.UsersRepository.GetByIdAsync(personId, cancellationToken)
            ?? throw new AgentConversationForbiddenException();

        if (requestedConversationId is { } conversationId)
        {
            return await ResolveExistingAsync(personId, conversationId, cancellationToken);
        }

        await EnsureCatalogsExistAsync(cancellationToken);
        var profiles = await unitOfWork.ChatUserProfilesRepository
            .GetByUserIdAsync(personId, cancellationToken);
        var profile = profiles.FirstOrDefault();
        if (profile is null)
        {
            profile = ChatUserProfile.Create(personId, null, null, null);
            await unitOfWork.ChatUserProfilesRepository.AddAsync(profile, cancellationToken);
        }

        var conversation = ChatConversation.Create(defaults.InitialConversationStatusId);
        var participant = ChatParticipant.Create(
            conversation.Id,
            defaults.ClientParticipantTypeId,
            chatUserProfileId: profile.Id);

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
        var profiles = await unitOfWork.ChatUserProfilesRepository
            .GetByUserIdAsync(personId, cancellationToken);
        var participants = await unitOfWork.ChatParticipantsRepository
            .GetAllByConversationIdAsync(conversationId, cancellationToken);
        var profileIds = profiles.Select(profile => profile.Id).ToHashSet();
        var isParticipant = participants.Any(participant =>
            participant.ChatUserProfileId is { } profileId &&
            profileIds.Contains(profileId));
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
        var status = await unitOfWork.ConversationStatusesRepository.GetByIdAsync(
            defaults.InitialConversationStatusId,
            cancellationToken);
        var participantType = await unitOfWork.SenderTypesRepository.GetByIdAsync(
            defaults.ClientParticipantTypeId,
            cancellationToken);
        if (status is null || participantType is null)
        {
            throw new AgentConversationConfigurationException();
        }
    }
}
