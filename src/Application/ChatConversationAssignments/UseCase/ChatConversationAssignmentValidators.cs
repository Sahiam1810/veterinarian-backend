using FluentValidation;

namespace Application.ChatConversationAssignments.UseCase;

public sealed class CreateChatConversationAssignmentCommandValidator
    : AbstractValidator<CreateChatConversationAssignmentCommand>
{
    public CreateChatConversationAssignmentCommandValidator()
    {
        RuleFor(command => command.ChatConversationId)
            .NotEmpty()
            .WithMessage("El identificador de la conversación es obligatorio.");
    }
}

public sealed class GetChatConversationAssignmentByConversationIdQueryValidator
    : AbstractValidator<GetChatConversationAssignmentByConversationIdQuery>
{
    public GetChatConversationAssignmentByConversationIdQueryValidator()
    {
        RuleFor(query => query.ChatConversationId)
            .NotEmpty()
            .WithMessage("El identificador de la conversación es obligatorio.");
    }
}

public sealed class GetChatConversationAssignmentsByAgentHumanIdQueryValidator
    : AbstractValidator<GetChatConversationAssignmentsByAgentHumanIdQuery>
{
    public GetChatConversationAssignmentsByAgentHumanIdQueryValidator()
    {
        RuleFor(query => query.AgentHumanId)
            .NotEmpty()
            .WithMessage("El identificador del agente humano es obligatorio.");
    }
}

public sealed class UpdateChatConversationAssignmentCommandValidator
    : AbstractValidator<UpdateChatConversationAssignmentCommand>
{
    public UpdateChatConversationAssignmentCommandValidator()
    {
        RuleFor(command => command.ChatConversationId)
            .NotEmpty()
            .WithMessage("El identificador de la conversación es obligatorio.");
    }
}

public sealed class DeleteChatConversationAssignmentCommandValidator
    : AbstractValidator<DeleteChatConversationAssignmentCommand>
{
    public DeleteChatConversationAssignmentCommandValidator()
    {
        RuleFor(command => command.ChatConversationId)
            .NotEmpty()
            .WithMessage("El identificador de la conversación es obligatorio.");
    }
}
