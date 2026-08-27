using FluentValidation;

namespace Application.ConversationStatuses.UseCases;

// Valida creación de estado de conversación.
public sealed class CreateConversationStatusCommandValidator : AbstractValidator<CreateConversationStatusCommand>
{
    public CreateConversationStatusCommandValidator() =>
        RuleFor(x => x.Name).NotEmpty().MaximumLength(50);
}

// Valida actualización de estado de conversación.
public sealed class UpdateConversationStatusCommandValidator : AbstractValidator<UpdateConversationStatusCommand>
{
    public UpdateConversationStatusCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(50);
    }
}
