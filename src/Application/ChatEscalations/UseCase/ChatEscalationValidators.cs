using FluentValidation;

namespace Application.ChatEscalations.UseCase;

public sealed class CreateChatEscalationCommandValidator
    : AbstractValidator<CreateChatEscalationCommand>
{
    public CreateChatEscalationCommandValidator()
    {
        RuleFor(command => command.ChatConversationId)
            .NotEmpty()
            .WithMessage("El identificador de la conversación es obligatorio.");

        RuleFor(command => command.EscalationStatusId)
            .NotEmpty()
            .WithMessage("El identificador del estado de escalamiento es obligatorio.");
    }
}

public sealed class GetChatEscalationByIdQueryValidator
    : AbstractValidator<GetChatEscalationByIdQuery>
{
    public GetChatEscalationByIdQueryValidator()
    {
        RuleFor(query => query.Id)
            .NotEmpty()
            .WithMessage("El identificador del escalamiento es obligatorio.");
    }
}

public sealed class GetChatEscalationsByConversationIdQueryValidator
    : AbstractValidator<GetChatEscalationsByConversationIdQuery>
{
    public GetChatEscalationsByConversationIdQueryValidator()
    {
        RuleFor(query => query.ChatConversationId)
            .NotEmpty()
            .WithMessage("El identificador de la conversación es obligatorio.");
    }
}

public sealed class UpdateChatEscalationCommandValidator
    : AbstractValidator<UpdateChatEscalationCommand>
{
    public UpdateChatEscalationCommandValidator()
    {
        RuleFor(command => command.Id)
            .NotEmpty()
            .WithMessage("El identificador del escalamiento es obligatorio.");

        RuleFor(command => command.EscalationStatusId)
            .NotEmpty()
            .WithMessage("El identificador del estado de escalamiento es obligatorio.");
    }
}

public sealed class DeleteChatEscalationCommandValidator
    : AbstractValidator<DeleteChatEscalationCommand>
{
    public DeleteChatEscalationCommandValidator()
    {
        RuleFor(command => command.Id)
            .NotEmpty()
            .WithMessage("El identificador del escalamiento es obligatorio.");
    }
}
