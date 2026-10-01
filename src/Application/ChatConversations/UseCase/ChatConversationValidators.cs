using FluentValidation;

namespace Application.ChatConversations.UseCase;

public sealed class CreateChatConversationCommandValidator : AbstractValidator<CreateChatConversationCommand>
{
    public CreateChatConversationCommandValidator()
    {
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
