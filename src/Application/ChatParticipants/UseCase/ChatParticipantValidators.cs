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
                command.ClientId,
                command.AgentHumanId))
            .WithMessage("El participante debe tener exactamente una identidad (cliente o agente humano).");

        RuleFor(command => command.ClientId)
            .Must(clientId => !clientId.HasValue || clientId.Value != Guid.Empty)
            .WithMessage("El identificador del cliente no puede ser vacío.");

        RuleFor(command => command.AgentHumanId)
            .Must(agentId => !agentId.HasValue || agentId.Value != Guid.Empty)
            .WithMessage("El identificador del agente humano no puede ser vacío.");

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
                command.ClientId,
                command.AgentHumanId))
            .WithMessage("El participante debe tener exactamente una identidad (cliente o agente humano).");

        RuleFor(command => command.ClientId)
            .Must(clientId => !clientId.HasValue || clientId.Value != Guid.Empty)
            .WithMessage("El identificador del cliente no puede ser vacío.");

        RuleFor(command => command.AgentHumanId)
            .Must(agentId => !agentId.HasValue || agentId.Value != Guid.Empty)
            .WithMessage("El identificador del agente humano no puede ser vacío.");

    }
}

internal static class ChatParticipantIdentityValidation
{
    internal static bool HasExactlyOneIdentity(Guid? clientId, Guid? agentHumanId) =>
        clientId.HasValue != agentHumanId.HasValue;
}
