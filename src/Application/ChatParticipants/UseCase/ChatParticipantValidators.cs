using FluentValidation;

namespace Application.ChatParticipants.UseCase;

public sealed class CreateChatParticipantCommandValidator
    : AbstractValidator<CreateChatParticipantCommand>
{
    public CreateChatParticipantCommandValidator()
    {
        RuleFor(command => command.ChatConversationId)
            .NotEmpty()
            .WithMessage("El identificador de la conversación es obligatorio.");

        RuleFor(command => command.ParticipantTypeId)
            .NotEmpty()
            .WithMessage("El identificador del tipo de participante es obligatorio.");

        RuleFor(command => command)
            .Must(command => ChatParticipantIdentityValidation.HasExactlyOneIdentity(
                command.ChatUserProfileId,
                command.AgentHumanId,
                command.AiModelId))
            .WithMessage("El participante debe tener exactamente una identidad (perfil de chat, agente humano o modelo de IA).");

        RuleFor(command => command.ChatUserProfileId)
            .Must(profileId => !profileId.HasValue || profileId.Value != Guid.Empty)
            .WithMessage("El identificador del perfil de chat no puede ser vacío.");

        RuleFor(command => command.AgentHumanId)
            .Must(agentId => !agentId.HasValue || agentId.Value != Guid.Empty)
            .WithMessage("El identificador del agente humano no puede ser vacío.");

        RuleFor(command => command.AiModelId)
            .Must(aiModelId => !aiModelId.HasValue || aiModelId.Value != Guid.Empty)
            .WithMessage("El identificador del modelo de IA no puede ser vacío.");
    }
}

public sealed class GetChatParticipantByIdQueryValidator
    : AbstractValidator<GetChatParticipantByIdQuery>
{
    public GetChatParticipantByIdQueryValidator()
    {
        RuleFor(query => query.Id)
            .NotEmpty()
            .WithMessage("El identificador del participante es obligatorio.");
    }
}

public sealed class GetChatParticipantsByConversationIdQueryValidator
    : AbstractValidator<GetChatParticipantsByConversationIdQuery>
{
    public GetChatParticipantsByConversationIdQueryValidator()
    {
        RuleFor(query => query.ChatConversationId)
            .NotEmpty()
            .WithMessage("El identificador de la conversación es obligatorio.");
    }
}

public sealed class ChangeChatParticipantIdentityCommandValidator
    : AbstractValidator<ChangeChatParticipantIdentityCommand>
{
    public ChangeChatParticipantIdentityCommandValidator()
    {
        RuleFor(command => command.Id)
            .NotEmpty()
            .WithMessage("El identificador del participante es obligatorio.");

        RuleFor(command => command)
            .Must(command => ChatParticipantIdentityValidation.HasExactlyOneIdentity(
                command.ChatUserProfileId,
                command.AgentHumanId,
                command.AiModelId))
            .WithMessage("El participante debe tener exactamente una identidad (perfil de chat, agente humano o modelo de IA).");

        RuleFor(command => command.ChatUserProfileId)
            .Must(profileId => !profileId.HasValue || profileId.Value != Guid.Empty)
            .WithMessage("El identificador del perfil de chat no puede ser vacío.");

        RuleFor(command => command.AgentHumanId)
            .Must(agentId => !agentId.HasValue || agentId.Value != Guid.Empty)
            .WithMessage("El identificador del agente humano no puede ser vacío.");

        RuleFor(command => command.AiModelId)
            .Must(aiModelId => !aiModelId.HasValue || aiModelId.Value != Guid.Empty)
            .WithMessage("El identificador del modelo de IA no puede ser vacío.");
    }
}

internal static class ChatParticipantIdentityValidation
{
    internal static bool HasExactlyOneIdentity(
        Guid? chatUserProfileId,
        Guid? agentHumanId,
        Guid? aiModelId)
    {
        var identityCount = 0;

        if (chatUserProfileId.HasValue)
        {
            identityCount++;
        }

        if (agentHumanId.HasValue)
        {
            identityCount++;
        }

        if (aiModelId.HasValue)
        {
            identityCount++;
        }

        return identityCount == 1;
    }
}
