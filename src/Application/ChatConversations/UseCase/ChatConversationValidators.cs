using FluentValidation;

namespace Application.ChatConversations.UseCase;

public sealed class CreateChatConversationCommandValidator : AbstractValidator<CreateChatConversationCommand>
{
    public CreateChatConversationCommandValidator()
    {
        RuleFor(command => command.ConversationStatusId)
            .NotEmpty()
            .WithMessage("El identificador del estado de conversación es obligatorio.");

        RuleFor(command => command.PriorityId)
            .Must(priorityId => !priorityId.HasValue || priorityId.Value != Guid.Empty)
            .WithMessage("El identificador de prioridad no puede ser vacío cuando se proporciona.");
    }
}

public sealed class GetChatConversationByIdQueryValidator : AbstractValidator<GetChatConversationByIdQuery>
{
    public GetChatConversationByIdQueryValidator()
    {
        RuleFor(query => query.Id)
            .NotEmpty()
            .WithMessage("El identificador de la conversación es obligatorio.");
    }
}

public sealed class UpdateChatConversationStatusCommandValidator
    : AbstractValidator<UpdateChatConversationStatusCommand>
{
    public UpdateChatConversationStatusCommandValidator()
    {
        RuleFor(command => command.Id)
            .NotEmpty()
            .WithMessage("El identificador de la conversación es obligatorio.");

        RuleFor(command => command.ConversationStatusId)
            .NotEmpty()
            .WithMessage("El identificador del estado de conversación es obligatorio.");
    }
}

public sealed class UpdateChatConversationPriorityCommandValidator
    : AbstractValidator<UpdateChatConversationPriorityCommand>
{
    public UpdateChatConversationPriorityCommandValidator()
    {
        RuleFor(command => command.Id)
            .NotEmpty()
            .WithMessage("El identificador de la conversación es obligatorio.");

        RuleFor(command => command.PriorityId)
            .Must(priorityId => !priorityId.HasValue || priorityId.Value != Guid.Empty)
            .WithMessage("El identificador de prioridad no puede ser vacío cuando se proporciona.");
    }
}

public sealed class UpdateChatConversationAiEnabledCommandValidator
    : AbstractValidator<UpdateChatConversationAiEnabledCommand>
{
    public UpdateChatConversationAiEnabledCommandValidator()
    {
        RuleFor(command => command.Id)
            .NotEmpty()
            .WithMessage("El identificador de la conversación es obligatorio.");
    }
}

public sealed class CloseChatConversationCommandValidator : AbstractValidator<CloseChatConversationCommand>
{
    public CloseChatConversationCommandValidator()
    {
        RuleFor(command => command.Id)
            .NotEmpty()
            .WithMessage("El identificador de la conversación es obligatorio.");

        RuleFor(command => command.ClosedBy)
            .Must(closedBy => closedBy != Guid.Empty)
            .When(command => command.ClosedBy.HasValue)
            .WithMessage("El identificador de cierre no puede ser vacío.");
    }
}

public sealed class ReopenChatConversationCommandValidator : AbstractValidator<ReopenChatConversationCommand>
{
    public ReopenChatConversationCommandValidator()
    {
        RuleFor(command => command.Id)
            .NotEmpty()
            .WithMessage("El identificador de la conversación es obligatorio.");
    }
}
